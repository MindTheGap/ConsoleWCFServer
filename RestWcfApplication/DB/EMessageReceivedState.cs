using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RestWcfApplication.DB
{
  [DataContract]
  [Flags]
  public enum EMessageReceivedState
  {
    MessageStateNotReceivedYet = 1,
    MessageStateSentToServer = 2,
    MessageStateSendToClient = 4
  }
}