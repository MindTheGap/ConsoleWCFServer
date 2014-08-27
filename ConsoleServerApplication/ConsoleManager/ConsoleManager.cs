using ChildsTubeConsoleServer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ChildsTubeConsoleServer.ViewModel;


namespace ChildsTubeConsoleServer.ConsoleManagerNS
{
    public static class ConsoleManager
    {
        #region Class Members

        private static string UsageString { get; set; }

        private static string ThreadPoolInformationString { get; set; }

        #endregion Class Members

        #region Class Management

        public static void InitObject(XElement ConfigurationXElement)
        {
            var consoleChunk = ConfigurationXElement.Element("Console");
            if (consoleChunk != null)
            {
                string strUsage;
                Helper.ReadValue(consoleChunk, "Usage", out strUsage);
                UsageString = strUsage;

                string strThreadPoolInformation;
                Helper.ReadValue(consoleChunk, "ThreadPoolInformation", out strThreadPoolInformation);
                ThreadPoolInformationString = strThreadPoolInformation;
            }
        }

        #endregion Class Management

        #region DB Management

        public static void SaveToDbHandler()
        {
          Console.WriteLine("Do you want to save before you exit?");
          string answer = Console.ReadLine();
          if (String.Compare(answer, "yes", StringComparison.OrdinalIgnoreCase) == 0 || String.Compare(answer, "1", StringComparison.OrdinalIgnoreCase) == 0)
          {
            try
            {
              if (MainWindowViewModel.MainDBEntities != null)
              {
                MainWindowViewModel.MainDBEntities.SaveChanges();
              }
            }
            catch (Exception e)
            {
              Console.WriteLine("ERROR while saving! Exception: " + e.Message + ".\n\nInner Exception: " + e.InnerException);
            }
          }
        }

        #endregion DB Management

        #region Handle Commands

        public static void ReadCommands()
        {
            while (true)
            {
                string strCommand = Console.ReadLine();
                HandleCommand(strCommand);
            }
        }

        public static void HandleCommand(string strCommand)
        {
            if (CompareLowerStrings(strCommand, "help", "usage", "?"))
            {
                Console.WriteLine(UsageString);
                Console.WriteLine();
            }
            else if (CompareLowerStrings(strCommand, "tp"))
            {
                int maxThreads, completionThreads;
                ThreadPool.GetMaxThreads(out maxThreads, out completionThreads);
                Console.WriteLine(ThreadPoolInformationString, maxThreads, completionThreads);
                Console.WriteLine();
            }
            else if (CompareLowerStrings(strCommand, "save"))
            {
              SaveToDbHandler();
              Console.WriteLine("Wrote to DB successfully!");
              Console.WriteLine();
            }
        }

        #endregion Handle Commands

        #region Helpers

        public static bool CompareLowerStrings(string string1, params string[] arrStrings)
        {
            if (string1 == null || arrStrings == null)
                return false;

            foreach (var item in arrStrings)
            {
                if (string1.ToLower().CompareTo(item) == 0)
                    return true;
            }

            return false;
        }

        #endregion Helpers
    }
}
