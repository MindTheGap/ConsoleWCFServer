using ChildsTubeConsoleServer.DB;
using ChildsTubeConsoleServer.Helpers;
using ChildsTubeConsoleServer.LogManagerNS;
using ChildsTubeConsoleServer.ThreadPoolManagerNS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ChildsTubeConsoleServer.ViewModel
{
    public class MainWindowViewModel
    {
        #region DB

        public static mainDBEntities MainDBEntities = new mainDBEntities();

        #endregion DB

        #region Data Members

        public LogManager LogManager { get; set; }

        #endregion Data Members

        public XElement ConfigurationXMLObject { get; set; }

        public ThreadPoolManager ThreadPoolManager { get; set; }

        public Thread Listener { get; set; }

        public MainWindowViewModel(XElement ConfigurationXMLObject)
        {
            try
            {
                this.ConfigurationXMLObject = ConfigurationXMLObject;

                InitObject();

                this.ThreadPoolManager = new ThreadPoolManager(this);

                Listener = new Thread(() => RequestListener(new string[] { "http://*:4297/" }));
                Listener.Name = "ListenerThread";
                Listener.IsBackground = true;
                Listener.Start();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: " + exception.Message);
            }
        }

        private void RequestListener(string[] prefixes)
        {
            if (prefixes.Length == 0)
            {
                LogManager.PrintErrorMessage("No prefixes!");
                return;
            }

            var listener = new HttpListener();

            foreach (string prefix in prefixes)
            {
                listener.Prefixes.Add(prefix);
            }

            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                ListenerCallback(context);
            }
        }

        private void ListenerCallback(HttpListenerContext result)
        {
            if (result != null)
            {
                HttpListenerRequest request = result.Request;

                if (request.HasEntityBody)
                {
                    ThreadPoolManager.AddTask(result);
                }
                else
                {
                    LogManager.PrintErrorMessage("Got message with no entity body!");
                }
            }
            else
            {
                LogManager.PrintErrorMessage("Got message with context null!");
            }
        }


        private void InitObject()
        {
            // Open App.Config of executable
            var configurationXMLObject = this.ConfigurationXMLObject;
            XElement logManagerChunk = configurationXMLObject.Element("LogManager");
            if (logManagerChunk != null)
            {
                this.LogManager = new LogManager();
                this.LogManager.Create(logManagerChunk);
            }
        }
    }
}
