using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using BplusDotNet;
using Cottle;
using Cottle.Commons;
using Cottle.Values;
using log4net.Util;

namespace SRS
{
    public class WebServer
    {
        private HttpListener _listener;
        private bool _firstRun = true;
        private const string Prefixes = "http://localhost:8975/";
        private const string CSSFile = "Resources\\site.css";
        private const string FaviconFile = "Resources\\favicon.ico";
        private BplusTree _tree;

        public WebServer(BplusTree serviceTree)
        {
            _tree = serviceTree;
        }

        public void Start()
        {
            if (_firstRun)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(Prefixes);
                _firstRun = false;
            }
            _listener.Start();
            Receive();
        }

        private string RenderTemplate(List<Service> services)
        {
            using (StreamReader template = new StreamReader(new FileStream("Resources\\services.cottle", FileMode.Open), Encoding.UTF8))
            {
                try
                {
                    Document document = new Document(template);
                    Scope scope = new Scope();
                    CommonFunctions.Assign(scope);
                    scope["services"] = new ReflectionValue(services);
                    return document.Render(scope); 
                }
                catch (Exception)
                {
                    Console.WriteLine("Error rendering template");
                }
            }
            return "Error rendering template";
        }

        private string GetCurrentServicesHTML()
        {
            List<Service> services = new List<Service>();
            string currentkey = _tree.FirstKey();
            while (currentkey != null)
            {
                if (_tree.ContainsKey(currentkey))
                {
                    Service tempService = new Service();
                    tempService.ParseXMLString(_tree[currentkey]);
                    services.Add(tempService);
                }
                currentkey = _tree.NextKey(currentkey);
            }
            return RenderTemplate(services);
        }

        private void Receive()
        {
            _listener.BeginGetContext(ListenerCallback, _listener);
        }


        private void ListenerCallback(IAsyncResult result)
        {
            if (!_listener.IsListening) return;
            HttpListenerContext context = _listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            string path = "";
            byte[] buffer = new byte[0];
            switch (request.RawUrl)
            {
                case "/":
                case "/root.html":
                    string responseString = GetCurrentServicesHTML();
                    buffer = Encoding.UTF8.GetBytes(responseString);
                    break;
                default:
                    if (request.RawUrl == "/site.css")
                    {
                        path = CSSFile;
                    }
                    if (request.RawUrl == "/favicon.ico")
                    {
                        path = FaviconFile;
                    }
                    if (!String.IsNullOrEmpty(path))
                    {
                        FileInfo fInfo = new FileInfo(path);
                        long numBytes = fInfo.Length;
                        FileStream fStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        BinaryReader br = new BinaryReader(fStream);
                        buffer = br.ReadBytes((int)numBytes);
                        br.Close();
                        fStream.Close();
                    }
                    break;
            }
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
            Receive();
        }
    }
}
