using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RestWcfApplication.DB
{
  [DataContract]
  public enum ESystemMessageState
  {
    SentSms, // server sent sms
    BothSidesAreIn, // both sides are in
    OneSideIsIn // only source user is in

  }
}