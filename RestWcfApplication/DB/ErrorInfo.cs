using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RestWcfApplication.DB
{
  [DataContract]
  public enum ErrorInfo
  {
    UserPhoneNumberDoesNotExist,
    UserIdDoesNotExist,
    PhoneNumberUserIdMismatch,
    BadVerificationCode,
    BadArgumentsLength
  }
}