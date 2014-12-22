using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RestWcfApplication.DB
{
  [DataContract]
  [Flags]
  public enum ESystemMessageState
  {
    RegisterSuccessfully = 1, // to let the user know a new user was created in DB
    VerificationCodeSent = 2, // to let the user know everything is going fine, server is just waiting for the verification code
    AlreadyRegisteredAndVerified = 4, // to let the client know of the UserId field
    SentSms = 8, // server sent sms
    BothSidesAreIn = 16, // both sides are in
    OneSideIsIn = 32, // only source user is in
    ClueNeeded = 64

  }
}