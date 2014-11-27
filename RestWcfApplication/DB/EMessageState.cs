using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RestWcfApplication.DB
{
  [DataContract]
  public enum EMessageState
  {
    Ok,
    Error,
    SentSms, // server sent sms
    BothSidesAreIn // both sides are in

  }
}