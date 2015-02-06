using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace RestWcfApplication.DB
{
  public static class ErrorDetails
  {
    public const string PhoneNumberDoesNotExist = "Phone Number does not exist";
    public const string UserIdDoesNotExist = "UserID does not exist";
    public const string BadVerificationCode = "Bad verification code";
    public const string BadArguments = "Bad arguments";
  }
}