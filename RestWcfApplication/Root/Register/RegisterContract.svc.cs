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
using RestWcfApplication.PushSharp;
using Twilio;

namespace RestWcfApplication.Root.Register
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class RegisterContract : IRegisterContract
  {
    public string VerifyValidationCode(Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var phoneNumber = jsonObject["phoneNumber"] as string;
        var validationCode = jsonObject["validationCode"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber);
          if (!userList.Any())
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.PhoneNumberDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          var user = userList.First();

          //if (user.VerificationCode != validationCode)
          //{
          //  toSend.Type = EMessagesTypesToClient.Error;
          //  toSend.ErrorInfo = ErrorInfo.BadVerificationCode.ToString("d");
          //  return CommManager.SendMessage(toSend);
          //}

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

    public string RegisterViaPhoneNumber(Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var phoneNumber = jsonObject["phoneNumber"] as string;

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

            //verificationCode = Twilio.Twilio.SendVerificationCode(phoneNumber);

            user.VerificationCode = verificationCode.ToString("d");
            context.SaveChanges();

            toSend.Type = EMessagesTypesToClient.SystemMessage;
            toSend.SystemMessage = ESystemMessageState.VerificationCodeSent;
            toSend.UserId = user.Id.ToString("d");
            return CommManager.SendMessage(toSend);
            //}
          }

          //verificationCode = Twilio.Twilio.SendVerificationCode(phoneNumber);

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

    public string RegisterUserDetailsFacebookDetails(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var firstName = jsonObject["firstName"];
        var lastName = jsonObject["lastName"];
        var facebookUserId = jsonObject["facebookUserId"];
        var email = jsonObject["email"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.FirstName = firstName;
          sourceUser.LastName = lastName;
          sourceUser.Email = email;
          sourceUser.FacebookUserId = facebookUserId;
          sourceUser.LastSeen = DateTime.Now.ToString("u");

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

    public string RegisterUserDetailsDeviceId(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var deviceId = jsonObject["deviceId"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.DeviceId = deviceId;
          sourceUser.LastSeen = DateTime.Now.ToString("u");

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

    public string TruncateAll()
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Database.ExecuteSqlCommand("DELETE FROM FirstMessage");
          context.Database.ExecuteSqlCommand("DELETE FROM Message");
          context.Database.ExecuteSqlCommand("DELETE FROM Hint");

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

    public string HelloPush(string deviceId, Stream data)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(data);
        var text = reader.ReadToEnd();

        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(text);
        if (jsonObject == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var alert = jsonObject["alert"];

        PushManager.PushToIos(deviceId, alert);
      
        toSend.Type = EMessagesTypesToClient.Ok;

        return CommManager.SendMessage(toSend);
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
  }
}
