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
            private const string RootPage = "root.html";
            private const string CSSFile = "site.css";
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
                Document document;
                Scope scope;

                using (StreamReader template = new StreamReader(new FileStream("Resources\\services.cottle", FileMode.Open), Encoding.UTF8))
                {
                    document = new Document(template); // may throw a ConfigException or DocumentException on error
                }

                scope = new Scope(); // create a new empty scope to store values
                CommonFunctions.Assign(scope); // see paragraph "Calling functions" about this line
                
                scope["services"] = new ReflectionValue(services);

                /* TODO: assign some values to scope */
                return document.Render(scope); // may throw a RenderException on error
            }

        private string GetCurrentServicesHTML()
        {
            StringBuilder sb = new StringBuilder();
            List<Service> services = new List<Service>();
            string currentkey = _tree.FirstKey();
            while (currentkey != null)
            {
                if (_tree.ContainsKey(currentkey))
                {
                    Service temp_service = new Service();
                    temp_service.ParseXMLString(_tree[currentkey]);
                    services.Add(temp_service);
                }
                currentkey = _tree.NextKey(currentkey);
            }
            return RenderTemplate(services);
        }

        private void Receive()
            {
                IAsyncResult result = _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
            }
            

            private void ListenerCallback(IAsyncResult result)
            {
                if (!_listener.IsListening) return;
                HttpListenerContext context = _listener.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string responseString = "";
                string path = "";
                switch (request.RawUrl)
                {
                    case "/":
                        responseString = GetCurrentServicesHTML();
                        break;
                    case "/root.html":
                        responseString = GetCurrentServicesHTML();
                        break;
                    case "/site.css":
                        path = CSSFile;
                        StreamReader streamReader = new StreamReader(path);
                        responseString = streamReader.ReadToEnd();
                        break;
                }
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                
                Receive();
            }
    } 
    }
