using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChildsTubeConsoleServer.Helpers
{
    public static class Helper
    {
        public static void ReadValue(XElement xElement, string strValue, out string value, string strDefault)
        {
            if (xElement != null)
            {
                var chunk = xElement;
                XElement valueElement = chunk.Element(strValue);
                if (valueElement == null)
                    value = valueElement.Value.ToString();
                else
                    value = strDefault;

                return;
            }

            value = null;
        }

        public static void ReadValue(XElement xElement, string strValue, out string value)
        {
            if (xElement != null)
            {
                var chunk = xElement;
                XElement valueElement = chunk.Element(strValue);
                if (valueElement != null)
                {
                    value = valueElement.Value.ToString();
                    return;
                }
            }

            value = null;
        }

        public static void PrintErrorMessage(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void PrintWarningMessage(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        public static void PrintSuccessMessage(string message)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }
    }
}
