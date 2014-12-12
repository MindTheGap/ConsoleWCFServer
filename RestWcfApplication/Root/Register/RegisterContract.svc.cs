using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Net;
using System.ServiceModel.Activation;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using RestSharp.Deserializers;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Twilio;

namespace RestWcfApplication.Root.Register
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class RegisterContract : IRegisterContract
  {
    private const string AccountSid = "ACd63332ed17dd316250547fa9906174b3";
    private const string AuthToken = "8c0e45ea5def9d3f69da04b9c6e7b905";

    public string VerifyValidationCode(string phoneNumber, string validationCode)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          phoneNumber = Regex.Replace(phoneNumber, @"[-+ ()]", "");

          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber);
          if (!userList.Any())
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserPhoneNumberDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var user = userList.First();

          if (user.VerificationCode != validationCode)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.BadVerificationCode.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          user.LastSeen = DateTime.Now.ToString("u");
          user.Verified = true;

          context.SaveChanges();

          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string RegisterViaPhoneNumber(string phoneNumber)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        phoneNumber = Regex.Replace(phoneNumber, @"[-+ ()]", "");

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          int verificationCode = -1;
          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber);
          var user = userList.FirstOrDefault();
          if (user != null)
          {
            //if (user.Verified)
            //{
            //  toSend.Type = EMessagesTypesToClient.SystemMessage;
            //  toSend.SystemMessage = ESystemMessageState.AlreadyRegisteredAndVerified;
            //  toSend.UserId = user.Id.ToString("d");
            //  return CommManager.SendMessage(toSend);
            //}
            //else
            //{
            user.Verified = false;

            verificationCode = SendVerificationCode(phoneNumber);

            user.VerificationCode = verificationCode.ToString("d");
            context.SaveChanges();

            toSend.Type = EMessagesTypesToClient.SystemMessage;
            toSend.SystemMessage = ESystemMessageState.VerificationCodeSent;
            toSend.UserId = user.Id.ToString("d");
            return CommManager.SendMessage(toSend);
            //}
          }

          verificationCode = SendVerificationCode(phoneNumber);

          var newUser = new User()
          {
            PhoneNumber = phoneNumber, 
            Verified = false, 
            VerificationCode = verificationCode.ToString("d")
          };
          context.Users.Add(newUser);
          context.SaveChanges();

          toSend.Type = EMessagesTypesToClient.SystemMessage;
          toSend.SystemMessage = ESystemMessageState.RegisterSuccessfully | ESystemMessageState.VerificationCodeSent;
          toSend.UserId = newUser.Id.ToString("d");
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    private static int SendVerificationCode(string phoneNumber)
    {
      var twilio = new TwilioRestClient(AccountSid, AuthToken);
      var random = new Random();
      var verificationCode = random.Next(100000, 999999);
      var message = twilio.SendMessage("+15098226878", "+" + phoneNumber, "Verification Code: " + verificationCode);
      if (message.ErrorCode != null)
      {
        throw new Exception(message.ErrorMessage);
      }
      return verificationCode;
    }

    public string RegisterUserDetails(string userId, string phoneNumber, string fbUserId, string email, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        phoneNumber = Regex.Replace(phoneNumber, @"[-+ ()]", "");
        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var firstName = jsonObject["firstName"];
        var lastName = jsonObject["lastName"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == phoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var userList = context.Users.Where(u => u.Id == userIdParsed);
          if (userList.Any())
          {
            var user = userList.First();
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.FacebookUserId = fbUserId;
            user.LastSeen = DateTime.Now.ToString("u");
          }

          context.SaveChanges();

          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.Exception = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string Hello()
    {
      dynamic toSend = new ExpandoObject();
      toSend.Type = "helloType";
      toSend.MessageType = 4;
      toSend.Message = "I love you";

      string responseString = JsonConvert.SerializeObject(toSend, Formatting.None,
                        new JsonSerializerSettings()
                        {
                          ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });

      return responseString;
    }
  }
}
