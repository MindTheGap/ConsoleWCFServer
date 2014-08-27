using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChildsTubeConsoleServer.Models
{
    public class Log
    {
        #region Properties

        public string Message { get; set; }
        public DateTime DateTime { get; set; }

        #endregion Properties

        #region Ctor

        public Log(string message)
        {
            this.Message = message;
            this.DateTime = DateTime.Now;
        }

        #endregion Ctor

        #region Overriden Functions

        public override string ToString()
        {
            return this.DateTime.ToString() + ": " + this.Message;
        }

        #endregion Overriden Functions
    }
}
