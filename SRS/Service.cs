#region

using System;
using System.Collections.Generic;
using System.Xml.Linq;

#endregion

namespace SRS
{
    public class Service
    {
        public Service()
        {
            InitialiseVariables();
        }

        public Service(string filename)
        {
            InitialiseVariables();
            ParseXMLFile(filename);
        }

        private Dictionary<string, string> ServiceInformation { get; set; }
        private Dictionary<string, string> ServiceConfig { get; set; }
        private Dictionary<string, string> SIPHeaders { get; set; }
        private Dictionary<string, string> SIPResponses { get; set; }
        private Dictionary<string, string> Capabilities { get; set; }
        private Dictionary<string, string> Metrics { get; set; }

        private void ParseXMLFile(string filename)
        {
            ParseXML(XDocument.Load(filename));
        }

        public void ParseXMLString(string xml)
        {
            ParseXML(XDocument.Parse(xml));
        }

        private void ParseXML(XDocument doc)
        {
            foreach (XElement block in doc.Elements("Service"))
            {
                foreach (XElement element in block.Elements("Service_Information").Elements())
                {
                    ServiceInformation[element.Name.ToString()] = element.Value;
                }
                foreach (XElement element in block.Elements("Service_Config").Elements())
                {
                    ServiceConfig[element.Name.ToString()] = element.Value;
                }
                foreach (XElement element in block.Elements("SIP_Headers").Elements())
                {
                    SIPHeaders[element.Name.ToString()] = element.Value;
                }
                foreach (XElement element in block.Elements("SIP_Responses").Elements())
                {
                    SIPResponses[element.Name.ToString()] = element.Value;
                }
                foreach (XElement element in block.Elements("Capabalities").Elements())
                {
                    Capabilities[element.Name.ToString()] = element.Value;
                }
                foreach (XElement element in block.Elements("Metrics").Elements())
                {
                    Metrics[element.Name.ToString()] = element.Value;
                }
            }
        }

        private void InitialiseVariables()
        {
            ServiceInformation = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ServiceConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SIPHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SIPResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Capabilities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Metrics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}