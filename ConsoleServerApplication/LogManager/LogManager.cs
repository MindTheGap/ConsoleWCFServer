using ChildsTubeConsoleServer.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ChildsTubeConsoleServer.LogManagerNS
{
    public class LogManager
    {
        #region Log Files

        #region Log File Names

        public string SuccessLogFileName { get; set; }
        public string FailureLogFileName { get; set; }
        public string WarningLogFileName { get; set; }
        public string AllLogFileName { get; set; }

        #endregion Log File Names

        #region Log File Streams

        public StreamWriter SuccessLogFileStream { get; set; }
        public StreamWriter FailureLogFileStream { get; set; }
        public StreamWriter WarningLogFileStream { get; set; }
        public StreamWriter AllLogFileStream { get; set; }

        #endregion Log File Streams

        #endregion Log Files

        public void Create(XElement logManagerXElement)
        {
            InitObject(logManagerXElement);

            LoadLogFileObjects();
        }

        public void InitObject(XElement logManagerXElement)
        {
            string strRelativePath;
            Helper.ReadValue(logManagerXElement, "RelativePath", out strRelativePath);

            var fileNamesChunk = logManagerXElement.Element("FileNames");
            if (fileNamesChunk != null)
            {
                string strSuccess, strFailure, strWarning, strAll;
                Helper.ReadValue(fileNamesChunk, "SuccessLogFileName", out strSuccess);
                Helper.ReadValue(fileNamesChunk, "FailureLogFileName", out strFailure);
                Helper.ReadValue(fileNamesChunk, "WarningLogFileName", out strWarning);
                Helper.ReadValue(fileNamesChunk, "AllLogFileName", out strAll);

                this.SuccessLogFileName = Path.Combine(strRelativePath, strSuccess);
                this.FailureLogFileName = Path.Combine(strRelativePath, strFailure);
                this.WarningLogFileName = Path.Combine(strRelativePath, strWarning);
                this.AllLogFileName = Path.Combine(strRelativePath, strAll);
            }
        }

        private void LoadLogFileObjects()
        {
            try
            {
                SuccessLogFileStream = new StreamWriter(SuccessLogFileName);
                FailureLogFileStream = new StreamWriter(FailureLogFileName);
                WarningLogFileStream = new StreamWriter(WarningLogFileName);
                AllLogFileStream = new StreamWriter(AllLogFileName);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: " + exception.Message);
            }
        }

        public void PrintErrorMessage(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            FailureLogFileStream.WriteLine(message);
            AllLogFileStream.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public void PrintWarningMessage(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            WarningLogFileStream.WriteLine(message);
            AllLogFileStream.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public void PrintSuccessMessage(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            SuccessLogFileStream.WriteLine(message);
            AllLogFileStream.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }
    }
}
