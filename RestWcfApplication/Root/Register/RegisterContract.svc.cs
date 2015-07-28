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
using RestWcfApplication.Root.Shared;
using Twilio;

namespace RestWcfApplication.Root.Register
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                   ConcurrencyMode = ConcurrencyMode.Multiple)]
  public class RegisterContract : IRegisterContract
  {
    private const int DefaultCoinsForNewUsers = 0;

    public string VerifyValidationCode(Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        dynamic toSend;
        if (!SharedHelper.DeserializeObject(stream, out jsonObject, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var phoneNumber = jsonObject["phoneNumber"] as string;
        var validationCode = jsonObject["validationCode"];

        var user = SharedHelper.QueryForObject<User>("Users", u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.PhoneNumberDoesNotExist;
          return CommManager.SendMessage(toSend);
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(user);

          //if (user.VerificationCode != validationCode)
          //{
          //  toSend.Type = EMessagesTypesToClient.Error;
          //  toSend.ErrorInfo = ErrorInfo.BadVerificationCode.ToString("d");
          //  return CommManager.SendMessage(toSend);
          //}

          user.LastSeen = DateTime.Now.ToString("u");
          user.Verified = true;

          context.SaveChanges();
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        toSend.User = user;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string RegisterViaPhoneNumber(Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        dynamic toSend;
        if (!SharedHelper.DeserializeObject(stream, out jsonObject, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var phoneNumber = jsonObject["phoneNumber"] as string;

        var user = SharedHelper.QueryForObject<User>("Users", u => u.PhoneNumber == phoneNumber);

        var verificationCode = Twilio.Twilio.SendVerificationCode(phoneNumber);

        if (user != null)
        {
          using (var context = new Entities())
          {
            context.Configuration.ProxyCreationEnabled = false;

            context.Users.Attach(user);

            user.Verified = false;
            user.VerificationCode = verificationCode.ToString("d");

            context.SaveChanges();
          }
        }
        else
        {
          using (var context = new Entities())
          {
            context.Configuration.ProxyCreationEnabled = false;

            user = new User()
            {
              PhoneNumber = phoneNumber,
              Coins = DefaultCoinsForNewUsers,
              Verified = false,
              VerificationCode = verificationCode.ToString("d")
            };
            context.Users.Add(user);

            context.SaveChanges();
          }
        }

        toSend.Type = EMessagesTypesToClient.SystemMessage;
        toSend.SystemMessage = ESystemMessageState.VerificationCodeSent;
        toSend.UserId = user.Id.ToString("d");
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string RegisterUserInformation(string userId, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var name = jsonObject.ContainsKey("name") ? jsonObject["name"] : null;
        var firstName = jsonObject.ContainsKey("firstName") ? jsonObject["firstName"] : null;
        var lastName = jsonObject.ContainsKey("lastName") ? jsonObject["lastName"] : null;
        var facebookUserId = jsonObject.ContainsKey("facebookUserId") ? jsonObject["facebookUserId"] : null;
        var email = jsonObject.ContainsKey("email") ? jsonObject["email"] : null;
        var deviceId = jsonObject.ContainsKey("deviceId") ? jsonObject["deviceId"] : null;
        var contacts = jsonObject.ContainsKey("contacts") ? jsonObject["contacts"] : null;
        var profileImageLink = jsonObject.ContainsKey("profileImageLink") ? jsonObject["profileImageLink"] : null;

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(sourceUser);

          if (name != null) sourceUser.DisplayName = name;
          if (firstName != null) sourceUser.FirstName = firstName;
          if (lastName != null) sourceUser.LastName = lastName;
          if (email != null) sourceUser.Email = email;
          if (facebookUserId != null) sourceUser.FacebookUserId = facebookUserId;
          if (deviceId != null) sourceUser.DeviceId = deviceId;
          if (profileImageLink != null) sourceUser.ProfileImageLink = profileImageLink;

          context.SaveChanges();
        }

        var anyNotifications = false;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          anyNotifications = context.Notifications.Any(n => n.UserId == sourceUserId);
        }

        var sourceUserName = firstName != null ? firstName + " " + lastName : name;

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          if (contacts != null)
          {
            foreach (var phoneNumbers in contacts)
            {
              foreach (string phoneNumber in phoneNumbers)
              {
                var contactUser = context.Users.SingleOrDefault(u => u.PhoneNumber == phoneNumber);
                if (contactUser != null)
                {
                  if (contactUser.DeviceId != null)
                  {
                    if (!context.Messages.Any(m => m.SourceUserId == sourceUserId || m.TargetUserId == sourceUserId))
                    {
                      PushManager.PushToIos(contactUser.DeviceId,
                        string.Format("{0} just joined IAmInToo! Feel free to be into them!", sourceUserName));
                    }
                  }

                  break;
                }
              }
            }
          }
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          if (!anyNotifications)
          {
            var newNotification = new DB.Notification
            {
              UserId = sourceUser.Id,
              Text = "Welcome to IAmInToo!"
            };

            context.Notifications.Add(newNotification);
          }

          context.SaveChanges();
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string RegisterUserFbInformation(string userId, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var firstName = jsonObject["firstName"];
        var lastName = jsonObject["lastName"];
        var facebookUserId = jsonObject["facebookUserId"];
        var email = jsonObject["email"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(sourceUser);

          sourceUser.FirstName = firstName;
          sourceUser.LastName = lastName;
          sourceUser.Email = email;
          sourceUser.FacebookUserId = facebookUserId;
          sourceUser.LastSeen = DateTime.Now.ToString("u");

          context.SaveChanges();
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
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

          context.Database.ExecuteSqlCommand("DELETE FROM [Notification]");
          context.Database.ExecuteSqlCommand("DELETE FROM [Message]");
          context.Database.ExecuteSqlCommand("DELETE FROM [FirstMessage]");
          context.Database.ExecuteSqlCommand("DELETE FROM [Hint]");

          context.SaveChanges();
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string TruncateUsers()
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Database.ExecuteSqlCommand("DELETE FROM [User]");

          context.SaveChanges();
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
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
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }
  }
}
