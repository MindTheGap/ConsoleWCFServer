using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RestWcfApplication.Communications
{
  public enum EMessagesTypesToClient
  {
    Message = 1, // normal message sent
    SystemMessage = 2, // system message sent
    MultipleMessages = 4, // multiple messages sent
    MatchFound = 8, // normal message AND system message sent
    Ok = 16, // just sends OK to registration or something
    Error = 32
  }
}