using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Twilio;

namespace RestWcfApplication.Twilio
{
  public static class Twilio
  {
    private const string AccountSid = "ACd63332ed17dd316250547fa9906174b3";
    private const string AuthToken = "8c0e45ea5def9d3f69da04b9c6e7b905";
    private const string SourceNumber = "+15098226878";

    public static int SendVerificationCode(string phoneNumber)
    {
      var twilio = new TwilioRestClient(AccountSid, AuthToken);
      var random = new Random();
      var verificationCode = random.Next(100000, 999999);
      var message = twilio.SendMessage(SourceNumber, "+" + phoneNumber, "Verification Code: " + verificationCode);
      if (message.ErrorCode != null)
      {
        throw new Exception(message.ErrorMessage);
      }
      return verificationCode;
    }

    public static void SendInvitationMessage(string sourceUserName, string targetPhoneNumber)
    {
      var twilio = new TwilioRestClient(AccountSid, AuthToken);
      var stringFormat = string.Format(
          @"{0} has invited you to join IAmInToo application. You are more than welcome to click on the following link and join: http://store.apple.com/iamintoo",
          sourceUserName);
      var message = twilio.SendMessage(SourceNumber, "+" + targetPhoneNumber, stringFormat);
      if (message.ErrorCode != null)
      {
        throw new Exception(message.ErrorMessage);
      }
    }
  }
}