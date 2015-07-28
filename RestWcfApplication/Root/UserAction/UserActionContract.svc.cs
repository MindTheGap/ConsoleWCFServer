using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Newtonsoft.Json;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using RestWcfApplication.PushSharp;
using RestWcfApplication.Root.Shared;

namespace RestWcfApplication.Root.UserAction
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                   ConcurrencyMode = ConcurrencyMode.Multiple)]
  public class UserActionContract : IUserActionContract
  {
    public string GuessContactUser(string userId, System.IO.Stream stream)
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

        if (sourceUser.Coins < 5)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.InsufficientFunds;
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var targetUserId = (int)jsonObject["targetUserId"];
        var phoneNumbers = jsonObject["phoneNumbers"].ToObject<List<string>>();

        if (phoneNumbers == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var realTargetUser = SharedHelper.QueryForObject<User>("Users", user => user.Id == targetUserId);
        if (realTargetUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages", firstMessage => firstMessage.SourceUserId == targetUserId && firstMessage.TargetUserId == sourceUserId);
        if (initialMessage == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var guessedCorrectly = phoneNumbers.Contains(realTargetUser.PhoneNumber);
        User guessedTargetUser = null;
        if (!guessedCorrectly)
        {
          guessedTargetUser = SharedHelper.QueryForObject<User>("Users", message => phoneNumbers.Contains(message.PhoneNumber));
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          if (!guessedCorrectly) context.Users.Attach(guessedTargetUser);
          context.Users.Attach(sourceUser);
          context.Users.Attach(realTargetUser);
          context.FirstMessages.Attach(initialMessage);

          sourceUser.Coins -= 5;
          initialMessage.GuessesUsed++;

          var needsToPushToRealTargetUser = false;
          Notification newNotification = null;
          if (guessedCorrectly)
          {
            initialMessage.MatchFound = true;

            var fullSourceUserName = GetFullNameOrPhoneNumber(sourceUser);
            newNotification = new Notification()
            {
              UserId = targetUserId,
              SenderUserId = sourceUserId,
              Text = string.Format("{0} guessed you correctly! You can now chat!", fullSourceUserName)
            };

            context.Notifications.Add(newNotification);

            var fullTargetUserName = GetFullNameOrPhoneNumber(realTargetUser);
            newNotification = new Notification
            {
              UserId = sourceUserId,
              Text = string.Format("You guessed {0} correctly! You can now chat!", fullTargetUserName)
            };

            context.Notifications.Add(newNotification);
          }
          else
          {
            var fullName = GetFullNameOrPhoneNumber(sourceUser);
            if (guessedTargetUser != null && guessedTargetUser.Verified)
            {
              newNotification = new Notification
              {
                UserId = guessedTargetUser.Id,
                SenderUserId = sourceUserId,
                Text = string.Format("{0} guessed you while trying to guess someone else", fullName)
              };

              context.Notifications.Add(newNotification);
              needsToPushToRealTargetUser = true;
            }

            newNotification = new Notification
            {
              UserId = targetUserId,
              SenderUserId = sourceUserId,
              Text = string.Format("{0} guessed someone else while trying to guess you", fullName)
            };

            context.Notifications.Add(newNotification);
          }

          context.SaveChanges();

          if (realTargetUser.DeviceId != null)
          {
            PushManager.PushToIos(realTargetUser.DeviceId, "You have a new notification!");
          }
          if (needsToPushToRealTargetUser && guessedTargetUser.DeviceId != null)
          {
            PushManager.PushToIos(guessedTargetUser.DeviceId, "You have a new notification!");
          }

          if (guessedCorrectly) toSend.Notification = newNotification;
          toSend.Success = guessedCorrectly;
          toSend.Coins = sourceUser.Coins;
          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
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

    public string GuessFacebookContactUser(string userId, Stream stream)
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

        if (sourceUser.Coins < 5)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.InsufficientFunds;
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var targetUserId = (int)jsonObject["targetUserId"];
        var facebookObjectId = jsonObject["facebookObjectId"];

        if (facebookObjectId == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var realTargetUser = SharedHelper.QueryForObject<User>("Users", user => user.Id == targetUserId);
        if (realTargetUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages", f => f.SourceUserId == targetUserId && f.TargetUserId == sourceUserId);
        if (initialMessage == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        User guessedTargetUser = null;
        var guessedCorrectly = facebookObjectId == realTargetUser.FacebookUserId;
        if (!guessedCorrectly)
        {
          guessedTargetUser = SharedHelper.QueryForObject<User>("Users", u => u.FacebookUserId == facebookObjectId);
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          if (!guessedCorrectly) context.Users.Attach(guessedTargetUser);
          context.Users.Attach(sourceUser);
          context.Users.Attach(realTargetUser);
          context.FirstMessages.Attach(initialMessage);

          sourceUser.Coins -= 5;
          initialMessage.GuessesUsed++;

          var needsToPushToRealTargetUser = false;
          Notification newNotification;
          if (guessedCorrectly)
          {
            initialMessage.MatchFound = true;

            var fullSourceUserName = GetFullNameOrPhoneNumber(sourceUser);
            newNotification = new Notification()
            {
              UserId = targetUserId,
              SenderUserId = sourceUserId,
              Text = string.Format("{0} guessed you correctly! You can now chat!", fullSourceUserName)
            };

            context.Notifications.Add(newNotification);

            var fullTargetUserName = GetFullNameOrPhoneNumber(realTargetUser);
            newNotification = new Notification
            {
              UserId = sourceUserId,
              Text = string.Format("You guessed {0} correctly! You can now chat!", fullTargetUserName)
            };

            context.Notifications.Add(newNotification);
          }
          else
          {
            var fullName = GetFullNameOrPhoneNumber(sourceUser);
            if (guessedTargetUser != null && guessedTargetUser.Verified)
            {
              newNotification = new Notification
              {
                UserId = guessedTargetUser.Id,
                SenderUserId = sourceUserId,
                Text = string.Format("{0} guessed you while trying to guess someone else", fullName)
              };

              context.Notifications.Add(newNotification);
              needsToPushToRealTargetUser = true;
            }

            newNotification = new Notification
            {
              UserId = targetUserId,
              SenderUserId = sourceUserId,
              Text = string.Format("{0} guessed someone else while trying to guess you", fullName)
            };

            context.Notifications.Add(newNotification);
          }

          context.SaveChanges();

          if (realTargetUser.DeviceId != null)
          {
            PushManager.PushToIos(realTargetUser.DeviceId, "You have a new notification!");
          }
          if (needsToPushToRealTargetUser && guessedTargetUser.DeviceId != null)
          {
            PushManager.PushToIos(guessedTargetUser.DeviceId, "You have a new notification!");
          }

          if (guessedCorrectly) toSend.Notification = newNotification;
          toSend.Coins = sourceUser.Coins;
          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
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
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var initialMessageId = (int) jsonObject["initialMessageId"];

        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages", message => message.Id == initialMessageId);
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

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(sourceUser);
          context.FirstMessages.Attach(initialMessage);

          sourceUser.Coins -= 20;

          var newNotification = new Notification();
          newNotification.UserId = initialMessage.SourceUserId;
          newNotification.SenderUserId = sourceUserId;
          var fullName = GetFullNameOrPhoneNumber(sourceUser);
          newNotification.Text = string.Format("{0} opened chat with you!", fullName);

          context.Notifications.Add(newNotification);

          context.SaveChanges();
        }

        if (initialMessage.SourceUser.DeviceId != null)
        {
          PushManager.PushToIos(initialMessage.SourceUser.DeviceId, "You have a new notification!");
        }

        toSend.Coins = sourceUser.Coins;
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

    public string UserIsTyping(string userId, System.IO.Stream stream)
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
        var provider = CultureInfo.InvariantCulture;
        var initialMessageId = (int) jsonObject["initialMessageId"];
        DateTime currentTime = DateTime.ParseExact(jsonObject["currentTime"], "yyyy-MM-dd-HH-mm-ss", provider);

        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages", f => f.Id == initialMessageId);
        if (initialMessage == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var targetUserId = initialMessage.SourceUserId == sourceUserId
          ? initialMessage.TargetUserId
          : initialMessage.SourceUserId;

        var targetUser = SharedHelper.QueryForObject<User>("Users", u => u.Id == targetUserId);
        if (targetUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.FirstMessages.Attach(initialMessage);

          if (initialMessage.SourceUserId == sourceUserId)
          {
            initialMessage.LastTimeSourceUserTyped = currentTime;
          }
          else
          {
            initialMessage.LastTimeTargetUserTyped = currentTime;
          }

          context.SaveChanges();
        }

        if (targetUser.DeviceId != null)
        {
          PushManager.PushToIos(targetUser.DeviceId);
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
  }
}
