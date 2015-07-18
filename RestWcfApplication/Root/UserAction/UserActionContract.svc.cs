using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel.Activation;
using Newtonsoft.Json;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using RestWcfApplication.PushSharp;

namespace RestWcfApplication.Root.UserAction
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class UserActionContract : IUserActionContract
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
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          var initialMessage = context.FirstMessages.FirstOrDefault(f => f.SourceUserId == targetUserId && f.TargetUserId == userIdParsed);
          if (initialMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          if (sourceUser.Coins < 5)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.InsufficientFunds;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.Coins -= 5;
          initialMessage.GuessesUsed++;

          Notification newNotification;
          if (phoneNumbers.Contains(targetUser.PhoneNumber))
          {
            success = true;

            initialMessage.MatchFound = true;

            var fullName = GetFullNameOrPhoneNumber(targetUser);
            newNotification = new Notification
            {
              UserId = targetUserId,
              Text = string.Format("You guessed {0} correctly!", fullName)
            };

            context.Notifications.Add(newNotification);
          }
          else
          {
            var fullName = GetFullNameOrPhoneNumber(sourceUser);
            newNotification = new Notification
            {
              UserId = targetUserId,
              SenderUserId = userIdParsed,
              Text = string.Format("{0} guessed someone else while trying to guess you", fullName)
            };

            context.Notifications.Add(newNotification);
          }

          context.SaveChanges();

          if (targetUser.DeviceId != null)
          {
            PushManager.PushToIos(targetUser.DeviceId, "You have a new notification!");
          }

          toSend.Success = success;
          if (success)
          {
            toSend.Notification = newNotification;
          }
          toSend.Coins = sourceUser.Coins;
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

    public string GuessFacebookContactUser(string userId, Stream stream)
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
        var facebookObjectId = jsonObject["facebookObjectId"];

        if (facebookObjectId == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
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
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          var initialMessage = context.FirstMessages.FirstOrDefault(f => f.SourceUserId == targetUserId && f.TargetUserId == userIdParsed);
          if (initialMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          if (sourceUser.Coins < 5)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.InsufficientFunds;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.Coins -= 5;
          initialMessage.GuessesUsed++;

          Notification newNotification;
          if (facebookObjectId == targetUser.FacebookUserId)
          {
            success = true;

            initialMessage.MatchFound = true;

            var fullName = GetFullNameOrPhoneNumber(targetUser);
            newNotification = new Notification
            {
              UserId = targetUserId,
              Text = string.Format("You guessed {0} correctly!", fullName)
            };

            context.Notifications.Add(newNotification);
          }
          else
          {
            var fullName = GetFullNameOrPhoneNumber(sourceUser);
            newNotification = new Notification
            {
              UserId = targetUserId,
              SenderUserId = userIdParsed,
              Text = string.Format("{0} guessed someone else while trying to guess you", fullName)
            };

            context.Notifications.Add(newNotification);
          }

          context.SaveChanges();

          if (targetUser.DeviceId != null)
          {
            PushManager.PushToIos(targetUser.DeviceId, "You have a new notification!");
          }

          toSend.Success = success;
          if (success)
          {
            toSend.Notification = newNotification;
          }
          toSend.Coins = sourceUser.Coins;
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

    public string OpenChat(string userId, Stream stream)
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

        var initialMessageId = (int)jsonObject["initialMessageId"];

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

          var initialMessage = context.FirstMessages.SingleOrDefault(u => u.Id == initialMessageId);
          if (initialMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          if (sourceUser.Coins < 20)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.InsufficientFunds;
            return CommManager.SendMessage(toSend);
          }

          sourceUser.Coins -= 20;

          var newNotification = new Notification();
          newNotification.UserId = initialMessage.SourceUserId;
          newNotification.SenderUserId = userIdParsed;
          var fullName = GetFullNameOrPhoneNumber(sourceUser);
          newNotification.Text = string.Format("{0} opened chat with you!", fullName);

          context.Notifications.Add(newNotification);

          context.SaveChanges();

          if (initialMessage.SourceUser.DeviceId != null)
          {
            PushManager.PushToIos(initialMessage.SourceUser.DeviceId, "You have a new notification!");
          }

          toSend.Coins = sourceUser.Coins;
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

    public string UserIsTyping(string userId, System.IO.Stream stream)
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

        var provider = CultureInfo.InvariantCulture;
        var initialMessageId = (int)jsonObject["initialMessageId"];
        DateTime currentTime = DateTime.ParseExact(jsonObject["currentTime"], "yyyy-MM-dd-HH-mm-ss", provider);

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

          var initialMessage = context.FirstMessages.SingleOrDefault(f => f.Id == initialMessageId);
          if (initialMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          if (initialMessage.SourceUserId == userIdParsed)
          {
            initialMessage.LastTimeSourceUserTyped = currentTime;
          }
          else
          {
            initialMessage.LastTimeTargetUserTyped = currentTime;
          }

          var targetUserId = initialMessage.SourceUserId == userIdParsed
            ? initialMessage.TargetUserId
            : initialMessage.SourceUserId;

          var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserId);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          context.SaveChanges();

          if (targetUser.DeviceId != null)
          {
            PushManager.PushToIos(targetUser.DeviceId);
          }

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
  }
}
