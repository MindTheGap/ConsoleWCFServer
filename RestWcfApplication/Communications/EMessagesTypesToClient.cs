using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RestWcfApplication.Communications
{
  public enum EMessagesTypesToClient
  {
    Message, // normal message sent
    SystemMessage, // system message sent
    MatchFound, // normal message AND system message sent
    Ok, // just sends OK to registration or something
    Error
  }
}