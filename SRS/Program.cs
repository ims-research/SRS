using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml;
using SIPLib.SIP;
using SIPLib.Utils;
using log4net;
using BplusDotNet;

namespace SRS
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SIPApp));
        private static readonly ILog SessionLog = LogManager.GetLogger("SessionLogger");
        private static SIPApp _app;
        private static BplusTree _tree;
        private static List<string> _changedServices = new List<string>();

        public static SIPStack CreateStack(SIPApp app, string proxyIp = null, int proxyPort = -1)
        {
            SIPStack myStack = new SIPStack(app);
            if (proxyIp != null)
            {
                myStack.ProxyHost = proxyIp;
                myStack.ProxyPort = (proxyPort == -1) ? 5060 : proxyPort;
            }
            return myStack;
        }

        public static TransportInfo CreateTransport(string listenIp, int listenPort)
        {
            return new TransportInfo(IPAddress.Parse(listenIp), listenPort, System.Net.Sockets.ProtocolType.Udp);
        }

        static void AppResponseRecvEvent(object sender, SipMessageEventArgs e)
        {
            Log.Info("Response Received:" + e.Message);
            Message response = e.Message;
            string requestType = response.First("CSeq").ToString().Trim().Split()[1].ToUpper();
            switch (requestType)
            {
                case "INVITE":
                case "REGISTER":
                case "BYE":
                default:
                    Log.Info("Response for Request Type " + requestType + " is unhandled ");
                    break;
            }
        }

        static void AppRequestRecvEvent(object sender, SipMessageEventArgs e)
        {
            Log.Info("Request Received:" + e.Message);
            Message request = e.Message;
            switch (request.Method.ToUpper())
            {
                case "MESSAGE":
                case "INVITE":
                case "BYE":
                case "CANCEL":
                case "ACK":
                case "OPTIONS":
                case "REFER":
                case "SUBSCRIBE":
                case "NOTIFY":
                case "PUBLISH":
                    {
                        _app.Useragents.Add(e.UA);
                        Log.Info("Received Publish request with body:" + e.Message.Body);
                        if (request.First("Content-Type").ToString().ToUpper().Equals("APPLICATION/SERV_DESC+XML"))
                        {
                            ProcessReceivedService(request.Body);
                        }
                        Message m = e.UA.CreateResponse(200, "OK");
                        e.UA.SendResponse(m);
                        break;
                    }
                case "INFO":
                default:
                    {
                        Log.Info("Request with method " + request.Method.ToUpper() + " is unhandled");
                        break;
                    }
            }
        }

        public static void InitKeyValueStore()
        {
            try
            {
                if (File.Exists("services.dat") && File.Exists("services.tree"))
                {
                    _tree = BplusTree.ReOpen("services.tree", "services.dat");

                }
                else
                {
                    _tree = BplusTree.Initialize("services.tree", "services.dat", 36);
                }
            }
            catch (Exception)
            {
                Log.Error("Error initialising service tree");
                //_tree = BplusTree.Initialize("services.tree", "services.dat", 36);
            }

        }

        private static void ProcessReceivedService(string serviceXML)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(serviceXML);
            XmlNodeList guids = xmlDoc.GetElementsByTagName("GUID");
            string serviceGUID = guids[0].InnerText;
            if (_tree.ContainsKey(serviceGUID))
            {
                string currentService = _tree[serviceGUID];
                if (currentService != serviceXML)
                {
                    _tree[serviceGUID] = serviceXML;
                    _changedServices.Add(serviceGUID);
                }
            }
            else
            {
                _tree[serviceGUID] = serviceXML;
                _changedServices.Add(serviceGUID);
            }
            _tree.Commit();
        }

        private static void StartTimer()
        {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(SendServicesToUSPS);
            aTimer.Interval = 30000;
            aTimer.Enabled = true;
        }

        private static void SendServicesToUSPS(object sender, ElapsedEventArgs e)
        {
            if (_changedServices.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (string changedService in _changedServices)
                {
                    sb.Append(_tree[changedService]+"\n");
                }
                _changedServices.Clear();
                _app.SendMessage("<sip:usps@open-ims.test>", sb.ToString(), "APPLICATION/SERVLIST+XML");
            }
        }


        static void Main(string[] args)
        {
            TransportInfo localTransport = CreateTransport(Helpers.GetLocalIP(), 7242);
            _app = new SIPApp(localTransport);
            _app.RequestRecvEvent += new EventHandler<SipMessageEventArgs>(AppRequestRecvEvent);
            _app.ResponseRecvEvent += new EventHandler<SipMessageEventArgs>(AppResponseRecvEvent);
            const string scscfIP = "scscf.open-ims.test";
            const int scscfPort = 6060;
            InitKeyValueStore();
            SIPStack stack = CreateStack(_app, scscfIP, scscfPort);
            stack.Uri = new SIPURI("SRS@open-ims.test");
            StartTimer();
            WebServer wb = new WebServer(_tree);
            wb.Start();
            Console.WriteLine("Press \'q\' to quit");
            while (Console.Read() != 'q') ;
        }
    }
}
