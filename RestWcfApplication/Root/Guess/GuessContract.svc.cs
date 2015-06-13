using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using Newtonsoft.Json;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using RestWcfApplication.PushSharp;

namespace RestWcfApplication.Root.Guess
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class GuessContract : IGuessContract
  {
    public string GuessContactUser(string userId, System.IO.Stream stream)
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

        var targetUserId = (int)jsonObject["targetUserId"];
        var phoneNumbers = jsonObject["phoneNumbers"].ToObject<List<string>>();

        if (phoneNumbers == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

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

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var success = false;

          var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserId);
          if (targetUser != null)
          {
            if (phoneNumbers.Contains(targetUser.PhoneNumber))
            {
              success = true;

              var initialMessage =
                context.FirstMessages.FirstOrDefault(
                  f => f.SourceUserId == targetUserId && f.TargetUserId == userIdParsed);
              if (initialMessage != null)
              {
                initialMessage.MatchFound = true;
              }

              var newNotification = new Notification();
              newNotification.UserId = targetUserId;
              var fullName = GetFullNameOrPhoneNumber(targetUser);
              newNotification.Text = string.Format("You guessed {0} correctly!", fullName);

              context.Notifications.Add(newNotification);
            }
            else
            {
              var newNotification = new Notification();
              newNotification.UserId = targetUserId;
              newNotification.SenderUserId = userIdParsed;
              var fullName = GetFullNameOrPhoneNumber(sourceUser);
              newNotification.Text = string.Format("{0} guessed you while trying to guess someone else", fullName);

              context.Notifications.Add(newNotification);
            }
          }

          context.SaveChanges();

          if (!success)
          {
            if (targetUser != null && targetUser.DeviceId != null)
            {
              PushManager.PushToIos(targetUser.DeviceId, "You have a new notification!");
            }
          }
          else
          {
            if (sourceUser.DeviceId != null)
            {
              PushManager.PushToIos(sourceUser.DeviceId);
            }
          }

          toSend.Success = success;
          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.Error = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    private static string GetFullNameOrPhoneNumber(User user)
    {
      string fullName;
      if (!string.IsNullOrEmpty(user.FirstName) || !string.IsNullOrEmpty(user.LastName))
      {
        if (!string.IsNullOrEmpty(user.FirstName))
        {
          fullName = user.FirstName +
                     (!string.IsNullOrEmpty(user.LastName) ? " " + user.LastName : string.Empty);
        }
        else
        {
          fullName = user.LastName;
        }
      }
      else
      {
        fullName = user.PhoneNumber;
      }
      return fullName;
    }
  }
}
