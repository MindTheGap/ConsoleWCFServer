using System.Data.SqlClient;
using System.Runtime.InteropServices;
using ChildsTubeConsoleServer.ConsoleManagerNS;
using ChildsTubeConsoleServer.Helpers;
using ChildsTubeConsoleServer.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Windows;

namespace ChildsTubeConsoleServer
{
    class Program
    {
        static public MainWindowViewModel ViewModel { get; set; }

        public static XElement ConfigurationXMLObject { get; set; }

        static void Main(string[] args)
        {
            ConfigurationXMLObject = XElement.Load(Definitions.ConfigurationFileName);
            if (ConfigurationXMLObject == null)
            {
                Helper.PrintErrorMessage("Configuration file couldn't be loaded!");
                return;
            }

            ViewModel = new MainWindowViewModel(ConfigurationXMLObject);

            ConsoleManager.InitObject(ConfigurationXMLObject);

            Console.WriteLine("Listening... type 'help' for further usage.");

            ConsoleManager.ReadCommands(); // blocking command
        }
    }
}
