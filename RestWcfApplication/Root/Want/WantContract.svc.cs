using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using System.ServiceModel.Activation;
using System.Drawing;
using RestWcfApplication.Root.Shared;

namespace RestWcfApplication.Root.Want
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                   ConcurrencyMode = ConcurrencyMode.Multiple)]
  public class WantContract : IWantContract
  {
    private const string DefaultEmptyMessage = @"Guess Who...";
    private const string MatchFound = "Match Found";
    private const string OneSideIsIn = "Waiting for the other party to be in too. Feel free to send them clues and help them out";
    private const string SentSms = "We've sent a SMS and are now waiting for the other party to install the application";

    private bool IsStringEmpty(string s)
    {
      return string.IsNullOrEmpty(s) || s == "(null)";
    }

    public string UpdateIWantUserByPhoneNumber(string userId, string token, string targetPhoneNumber, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, token, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var guessLimitStr = jsonObject.ContainsKey("guessLimit") ? jsonObject["guessLimit"] as string : string.Empty;
        var guessLimit = 3;
        if (!string.IsNullOrEmpty(guessLimitStr))
        {
          guessLimit = int.Parse(guessLimitStr);
        }
        var hint = jsonObject["hint"];
        var hintImageLink = jsonObject.ContainsKey("hintImageLink") ? jsonObject["hintImageLink"] : null;
        var hintVideoLink = jsonObject.ContainsKey("hintVideoLink") ? jsonObject["hintVideoLink"] : null;

        var hintNotUsed = IsStringEmpty(hint) && IsStringEmpty(hintImageLink) && IsStringEmpty(hintVideoLink);

        FirstMessage initialMessage;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          initialMessage = context.FirstMessages.Include("TargetUser").SingleOrDefault(
            u => ((u.SourceUserId == sourceUserId && u.TargetUser.PhoneNumber == targetPhoneNumber)
                  || (u.TargetUserId == sourceUserId && u.SourceUser.PhoneNumber == targetPhoneNumber)));
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(sourceUser);

          sourceUser.Coins += 5;

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink,
            Text = hintNotUsed ? DefaultEmptyMessage : hint
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = sourceUserId,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
          };
          if (initialMessage == null)
          {
            var newInitialMessage = new DB.FirstMessage()
            {
              Date = newDate,
              SourceUserId = sourceUserId,
              MaximumGuesses = guessLimit,
              GuessesUsed = 0,
              SubjectName = @""
            };

            context.FirstMessages.Add(newInitialMessage);

            initialMessage = newInitialMessage;
          }
          else
          {
            context.FirstMessages.Attach(initialMessage);
          }

          newMessage.FirstMessage = initialMessage;

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          // check if target user exists in the system:
          //  if not, send him a SMS
          //  if so, check if he's also in the source user:
          //    if not, send him a message telling him someone (source user) is in him
          //    if so, love is in the air - connect chat between them
          var targetUser = SharedHelper.QueryForObject<User>("Users", u => u.PhoneNumber == targetPhoneNumber);
          if (targetUser == null)
          {
            // target user doesn't exist in the system yet
            // sending him an invitation sms
            Twilio.Twilio.SendInvitationMessage(sourceUser.FirstName + " " + sourceUser.LastName, targetPhoneNumber);

            var newTargetUser = new User() { PhoneNumber = targetPhoneNumber };

            newMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            initialMessage.TargetUser = newTargetUser;

            var newSystemMessageHintSms = new Hint()
            {
              Text = SentSms
            };
            var newSystemMessageSms = new DB.Message()
            {
              SourceUserId = sourceUserId,
              TargetUser = newTargetUser,
              FirstMessage = initialMessage,
              Hint = newSystemMessageHintSms,
              Date = newDate,
              SystemMessageState = 1,
              ReceivedState = (int)EMessageReceivedState.MessageStateSentToClient
            };

            context.Messages.Add(newSystemMessageSms);

            context.Users.Add(newTargetUser);

            context.SaveChanges();

            toSend.Type = (int)EMessagesTypesToClient.Ok;
            toSend.Coins = sourceUser.Coins;
            toSend.InitialMessage = initialMessage;
            toSend.SystemMessage = newSystemMessageSms;
            toSend.ChatMessage = newMessage;
            return CommManager.SendMessage(toSend);
          }

          context.Users.Attach(targetUser);

          // target user exists
          newMessage.TargetUserId = targetUser.Id;
          if (initialMessage.TargetUserId == 0)
          {
            initialMessage.TargetUserId = targetUser.Id;
            initialMessage.TargetUser = targetUser;
          }

          // checking if target user is in source user also
          if (targetUser.Id == sourceUserId)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            var newSystemMessageHintMatch = new Hint()
            {
              Text = MatchFound
            };
            var newSystemMessageMatch = new DB.Message()
            {
              SourceUserId = sourceUserId,
              TargetUser = targetUser,
              FirstMessage = initialMessage,
              Hint = newSystemMessageHintMatch,
              Date = newDate,
              SystemMessageState = 1,
              ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
            };

            context.Messages.Add(newSystemMessageMatch);
            initialMessage.MatchFound = true;

            context.SaveChanges();

            //targetUser.PhoneNumber = string.Empty;

            if (targetUser.DeviceId != null)
            {
              PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"Match was found!");
            }

            toSend.Type = (int)EMessagesTypesToClient.Ok;
            toSend.Coins = sourceUser.Coins;
            toSend.ChatMessage = newMessage;
            toSend.SystemMessage = newSystemMessageMatch;
            toSend.InitialMessage = initialMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          var newSystemMessageHint = new DB.Hint()
          {
            Text = OneSideIsIn
          };
          var newSystemMessage = new DB.Message()
          {
            SourceUserId = sourceUserId,
            TargetUserId = targetUser.Id,
            FirstMessage = initialMessage,
            Hint = newSystemMessageHint,
            Date = newDate,
            SystemMessageState = 1,
            ReceivedState = (int)EMessageReceivedState.MessageStateSentToClient
          };

          context.Messages.Add(newSystemMessage);

          context.SaveChanges();

          //targetUser.PhoneNumber = string.Empty;

          if (targetUser.DeviceId != null)
          {
            PushSharp.PushManager.PushToIos(targetUser.DeviceId, "Someone is in to you!");
          }

          toSend.Type = (int)EMessagesTypesToClient.Ok;
          toSend.Coins = sourceUser.Coins;
          toSend.ChatMessage = newMessage;
          toSend.SystemMessage = newSystemMessage;
          toSend.InitialMessage = initialMessage;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    public string UpdateIWantUserByFacebookId(string userId, string token, string facebookId, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, token, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var guessLimitStr = jsonObject.ContainsKey("guessLimit") ? jsonObject["guessLimit"] as string : string.Empty;
        var guessLimit = 3;
        if (!string.IsNullOrEmpty(guessLimitStr))
        {
          guessLimit = int.Parse(guessLimitStr);
        }
        var hint = jsonObject["hint"];
        var hintImageLink = jsonObject.ContainsKey("hintImageLink") ? jsonObject["hintImageLink"] : null;
        var hintVideoLink = jsonObject.ContainsKey("hintVideoLink") ? jsonObject["hintVideoLink"] : null;

        var targetUser = SharedHelper.QueryForObject<User>("Users", u => u.FacebookUserId == facebookId);
        if (targetUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
          return CommManager.SendMessage(toSend);
        }

        var hintNotUsed = IsStringEmpty(hint) && IsStringEmpty(hintImageLink) && IsStringEmpty(hintVideoLink);
        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages",
          u => ((u.SourceUserId == sourceUserId && u.TargetUser.Id == targetUser.Id)
                || (u.TargetUserId == sourceUserId && u.SourceUser.Id == targetUser.Id)));

        var messageExists = false;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          messageExists = context.Messages.Any(m => m.SourceUserId == targetUser.Id && m.TargetUserId == sourceUserId);
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(sourceUser);
          context.Users.Attach(targetUser);

          sourceUser.Coins += 5;

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink,
            Text = hintNotUsed ? DefaultEmptyMessage : hint
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = sourceUserId,
            TargetUserId = targetUser.Id,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int) EMessageReceivedState.MessageStateSentToServer
          };
          if (initialMessage == null)
          {
            initialMessage = new FirstMessage()
            {
              Date = newDate,
              SourceUserId = sourceUserId,
              TargetUserId = targetUser.Id,
              MaximumGuesses = guessLimit,
              GuessesUsed = 0,
              SubjectName = @""
            };

            context.FirstMessages.Add(initialMessage);
          }
          else
          {
            context.FirstMessages.Attach(initialMessage);
          }

          newMessage.FirstMessage = initialMessage;

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          //  check if target user is also in the source user:
          //    if not, send him a message telling him someone (source user) is in him
          //    if so, love is in the air - connect chat between them

          // checking if target user is in source user also
          if (messageExists)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            var newSystemMessageHintMatch = new DB.Hint()
            {
              Text = MatchFound
            };
            var newSystemMessageMatch = new DB.Message()
            {
              SourceUserId = sourceUserId,
              TargetUser = targetUser,
              FirstMessage = initialMessage,
              Hint = newSystemMessageHintMatch,
              Date = newDate,
              SystemMessageState = 1,
              ReceivedState = (int) EMessageReceivedState.MessageStateSentToServer
            };

            context.Messages.Add(newSystemMessageMatch);

            initialMessage.MatchFound = true;

            context.SaveChanges();

            //targetUser.PhoneNumber = string.Empty;

            if (targetUser.DeviceId != null)
            {
              PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"Match was found!");
            }

            toSend.Type = (int) EMessagesTypesToClient.Ok;
            toSend.Coins = sourceUser.Coins;
            toSend.ChatMessage = newMessage;
            toSend.SystemMessage = newSystemMessageMatch;
            toSend.InitialMessage = initialMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          var newSystemMessageHint = new DB.Hint()
          {
            Text = OneSideIsIn
          };
          var newSystemMessage = new DB.Message()
          {
            SourceUserId = sourceUserId,
            TargetUser = targetUser,
            FirstMessage = initialMessage,
            Hint = newSystemMessageHint,
            Date = newDate,
            SystemMessageState = 1,
            ReceivedState = (int) EMessageReceivedState.MessageStateSentToClient
          };

          context.Messages.Add(newSystemMessage);

          context.SaveChanges();

          //targetUser.PhoneNumber = string.Empty;

          if (targetUser.DeviceId != null)
          {
            PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"Someone is in to you!");
          }

          toSend.Type = (int) EMessagesTypesToClient.Ok;
          toSend.Coins = sourceUser.Coins;
          toSend.ChatMessage = newMessage;
          toSend.SystemMessage = newSystemMessage;
          toSend.InitialMessage = initialMessage;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    public string UpdateIWantUserExistingMessage(string userId, string token, string initialMessageId, Stream stream)
    {
      try
      {
        Dictionary<string, dynamic> jsonObject;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, token, stream, out jsonObject, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var sourceUserId = sourceUser.Id;
        var messageText = jsonObject.ContainsKey("text") ? jsonObject["text"] : null;
        var messageImageLink = jsonObject.ContainsKey("imageLink") ? jsonObject["imageLink"] : null;
        var messageVideoLink = jsonObject.ContainsKey("videoLink") ? jsonObject["videoLink"] : null;
        var thumbnailImage = jsonObject.ContainsKey("ThumbnailImage") ? Convert.FromBase64String(jsonObject["ThumbnailImage"]) : null;
        var size = jsonObject.ContainsKey("Size") ? jsonObject["Size"] : null;

        var initialMessageIdReal = Convert.ToInt32(initialMessageId);

        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages", fm => fm.Id == initialMessageIdReal);
        if (initialMessage == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var targetUserId = initialMessage.SourceUserId == sourceUserId ? initialMessage.TargetUserId : initialMessage.SourceUserId;
        var targetUser = SharedHelper.QueryForObject<User>("Users", u => u.Id == targetUserId);
        if (targetUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
          return CommManager.SendMessage(toSend);
        }

        DB.Message newMessage = null;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(sourceUser);
          context.Users.Attach(targetUser);
          context.FirstMessages.Attach(initialMessage);

          sourceUser.Coins += initialMessage.SourceUserId == sourceUserId ? 2 : -2;

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new Hint
          {
            Text = messageText,
            PictureLink = messageImageLink,
            VideoLink = messageVideoLink,
            ThumbnailImage = thumbnailImage,
            Size = size
          };
          newMessage = new DB.Message()
          {
            SourceUserId = sourceUserId,
            TargetUserId = targetUser.Id,
            FirstMessage = initialMessage,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int) EMessageReceivedState.MessageStateSentToServer
          };

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          context.SaveChanges();
        }

        //targetUser.PhoneNumber = string.Empty;

        if (targetUser.DeviceId != null)
        {
          PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"You have a new message!");
        }

        toSend.Type = (int)EMessagesTypesToClient.Ok;
        toSend.Coins = sourceUser.Coins;
        toSend.Message = newMessage;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }
  }
}
