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

namespace RestWcfApplication.Root.Want
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
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

    public string UpdateIWantUserByPhoneNumber(string userId, string targetPhoneNumber, Stream data)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(data);
        var text = reader.ReadToEnd();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
          if (jsonObject == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.text = text;
            toSend.ErrorInfo = ErrorDetails.BadArguments;
            return CommManager.SendMessage(toSend);
          }

          var hint = jsonObject["hint"];
          var hintImageLink = jsonObject.ContainsKey("hintImageLink") ? jsonObject["hintImageLink"] : null;
          var hintVideoLink = jsonObject.ContainsKey("hintVideoLink") ? jsonObject["hintVideoLink"] : null;

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
          sourceUser.Coins += 5;

          var hintNotUsed = IsStringEmpty(hint) && IsStringEmpty(hintImageLink) && IsStringEmpty(hintVideoLink);
          var initialMessage =
            context.FirstMessages.SingleOrDefault(u => ((u.SourceUserId == userIdParsed && u.TargetUser.PhoneNumber == targetPhoneNumber)
                                                    ||  (u.TargetUserId == userIdParsed && u.SourceUser.PhoneNumber == targetPhoneNumber)));

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink,
            Text = hintNotUsed ? DefaultEmptyMessage : hint
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
          };
          if (initialMessage == null)
          {
            var newInitialMessage = new DB.FirstMessage()
            {
              Date = newDate,
              SourceUserId = userIdParsed,
              SubjectName = @""
            };

            context.FirstMessages.Add(newInitialMessage);

            initialMessage = newInitialMessage;
          }
          newMessage.FirstMessage = initialMessage;

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          // check if target user exists in the system:
          //  if not, send him a SMS
          //  if so, check if he's also in the source user:
          //    if not, send him a message telling him someone (source user) is in him
          //    if so, love is in the air - connect chat between them
          var targetUser = context.Users.SingleOrDefault(u => u.PhoneNumber == targetPhoneNumber);
          if (targetUser == null)
          {
            // target user doesn't exist in the system yet
            // sending him an invitation sms
            Twilio.Twilio.SendInvitationMessage(sourceUser.FirstName + " " + sourceUser.LastName, targetPhoneNumber);

            var newTargetUser = new DB.User() { PhoneNumber = targetPhoneNumber };

            newMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            initialMessage.TargetUser = newTargetUser;

            var newSystemMessageHintSms = new DB.Hint()
            {
              Text = SentSms
            };
            var newSystemMessageSms = new DB.Message()
            {
              SourceUserId = userIdParsed,
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

          // target user exists
          newMessage.TargetUserId = targetUser.Id;
          if (initialMessage.TargetUserId == 0)
          {
            initialMessage.TargetUserId = targetUser.Id;
            initialMessage.TargetUser = targetUser;
          }

          // checking if target user is in source user also
          if (initialMessage.TargetUserId == userIdParsed)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            var newSystemMessageHintMatch = new DB.Hint()
            {
              Text = MatchFound
            };
            var newSystemMessageMatch = new DB.Message()
            {
              SourceUserId = userIdParsed,
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
            SourceUserId = userIdParsed,
            TargetUser = targetUser,
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

    public string UpdateIWantUserByFacebookId(string userId, string facebookId, Stream data)
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

        var hint = jsonObject["hint"];
        var hintImageLink = jsonObject.ContainsKey("hintImageLink") ? jsonObject["hintImageLink"] : null;
        var hintVideoLink = jsonObject.ContainsKey("hintVideoLink") ? jsonObject["hintVideoLink"] : null;

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
          sourceUser.Coins += 5;

          var targetUser = context.Users.FirstOrDefault(u => u.FacebookUserId == facebookId);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          var hintNotUsed = IsStringEmpty(hint) && IsStringEmpty(hintImageLink) && IsStringEmpty(hintVideoLink);
          var firstMessage =
            context.FirstMessages.SingleOrDefault(u => ((u.SourceUserId == userIdParsed && u.TargetUser.Id == targetUser.Id)
                                                    || (u.TargetUserId == userIdParsed && u.SourceUser.Id == targetUser.Id)));

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink,
            Text = hintNotUsed ? DefaultEmptyMessage : hint
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            TargetUserId = targetUser.Id,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
          };
          if (firstMessage == null)
          {
            firstMessage = new FirstMessage()
            {
              Date = newDate,
              SourceUserId = userIdParsed,
              TargetUserId = targetUser.Id,
              SubjectName = @""
            };

            context.FirstMessages.Add(firstMessage);
          }
          newMessage.FirstMessage = firstMessage;

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          //  check if target user is also in the source user:
          //    if not, send him a message telling him someone (source user) is in him
          //    if so, love is in the air - connect chat between them

          // checking if target user is in source user also
          var messageExists = context.Messages.Any(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
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
              SourceUserId = userIdParsed,
              TargetUser = targetUser,
              FirstMessage = firstMessage,
              Hint = newSystemMessageHintMatch,
              Date = newDate,
              SystemMessageState = 1,
              ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
            };

            context.Messages.Add(newSystemMessageMatch);

            firstMessage.MatchFound = true;

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
            toSend.InitialMessage = firstMessage;
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
            SourceUserId = userIdParsed,
            TargetUser = targetUser,
            FirstMessage = firstMessage,
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
            PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"Someone is in to you!");
          }

          toSend.Type = (int)EMessagesTypesToClient.Ok;
          toSend.Coins = sourceUser.Coins;
          toSend.ChatMessage = newMessage;
          toSend.SystemMessage = newSystemMessage;
          toSend.InitialMessage = firstMessage;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    public string UpdateIWantUserExistingMessage(string userId, string initialMessageId, Stream data)
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

        var messageText = jsonObject.ContainsKey("text") ? jsonObject["text"] : null;
        var messageImageLink = jsonObject.ContainsKey("imageLink") ? jsonObject["imageLink"] : null;
        var messageVideoLink = jsonObject.ContainsKey("videoLink") ? jsonObject["videoLink"] : null;
        var thumbnailImage = jsonObject.ContainsKey("ThumbnailImage") ? Convert.FromBase64String(jsonObject["ThumbnailImage"]) : null;

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

          var initialMessageIdReal = Convert.ToInt32(initialMessageId);
          var firstMessage = context.FirstMessages.SingleOrDefault(fm => fm.Id == initialMessageIdReal);
          if (firstMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          if (firstMessage.SourceUserId == userIdParsed)
          {
            sourceUser.Coins += 2;
          }
          else
          {
            sourceUser.Coins -= 2;
          }

          var targetUserId = firstMessage.SourceUserId == userIdParsed ? firstMessage.TargetUserId : firstMessage.SourceUserId;
          var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserId);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            Text = messageText,
            PictureLink = messageImageLink,
            VideoLink = messageVideoLink,
            ThumbnailImage = thumbnailImage
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            TargetUserId = targetUser.Id,
            FirstMessage = firstMessage,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
          };

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          // target user does not want source user yet so:
          //  sending target user the message from source user

          context.SaveChanges();

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
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }
  }
}
