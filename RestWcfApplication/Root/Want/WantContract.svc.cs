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

namespace RestWcfApplication.Root.Want
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class WantContract : IWantContract
  {
    private const string DefaultClue = @"I need another clue...";
    private const string DefaultEmptyMessage = @"Guess Who...";

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
            toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var hint = jsonObject["hint"];
          var hintImageLink = jsonObject["hintImageLink"];
          var hintVideoLink = jsonObject["hintVideoLink"];

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

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
              Message = newMessage,
              SourceUserId = userIdParsed,
              SubjectName = @""
            };

            context.FirstMessages.Add(newInitialMessage);

            initialMessage = newInitialMessage;
          }

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
            //Twilio.Twilio.SendInvitationMessage(sourceUser.FirstName + " " + sourceUser.LastName, targetPhoneNumber);

            var newTargetUser = new DB.User() { PhoneNumber = targetPhoneNumber };

            newMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            initialMessage.TargetUser = newTargetUser;
            newMessage.SystemMessageState = (int) ESystemMessageState.SentSms;

            context.Users.Add(newTargetUser);

            context.SaveChanges();

            toSend.Type = (int)EMessagesTypesToClient.Ok;
            toSend.InitialMessage = initialMessage;
            toSend.ChatMessage = newMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user exists
          newMessage.TargetUserId = targetUser.Id;
          initialMessage.TargetUserId = targetUser.Id;

          // checking if target user is in source user also
          if (initialMessage.TargetUserId == userIdParsed)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            newMessage.SystemMessageState = (int)ESystemMessageState.BothSidesAreIn;
            initialMessage.MatchFound = true;

            context.SaveChanges();

            toSend.Type = (int)EMessagesTypesToClient.Ok;
            toSend.ChatMessage = newMessage;
            toSend.InitialMessage = initialMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          newMessage.SystemMessageState = (int)ESystemMessageState.OneSideIsIn;

          context.SaveChanges();

          if (targetUser.DeviceId != null)
          {
            PushSharp.PushManager.PushToIos(targetUser.DeviceId, "Someone is in to you!");
          }

          toSend.Type = (int)EMessagesTypesToClient.Ok;
          toSend.ChatMessage = newMessage;
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
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var hint = jsonObject["hint"];
        var hintImageLink = jsonObject["hintImageLink"];
        var hintVideoLink = jsonObject["hintVideoLink"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var targetUser = context.Users.SingleOrDefault(u => u.FacebookUserId == facebookId);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
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
            Text = hintNotUsed ? DefaultEmptyMessage : text
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
            var newFirstMessage = new DB.FirstMessage()
            {
              Date = newDate,
              Message = newMessage,
              SourceUserId = userIdParsed,
              TargetUserId = targetUser.Id,
              SubjectName = @""
            };

            context.FirstMessages.Add(newFirstMessage);

            firstMessage = newFirstMessage;
          }

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          //  check if target user is also in the source user:
          //    if not, send him a message telling him someone (source user) is in him
          //    if so, love is in the air - connect chat between them

          // checking if target user is in source user also
          var message = context.Messages.SingleOrDefault(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
          if (message != null)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            newMessage.SystemMessageState = (int)ESystemMessageState.BothSidesAreIn;
            firstMessage.MatchFound = true;

            context.SaveChanges();

            toSend.Type = (int) EMessagesTypesToClient.Ok;
            toSend.Message = newMessage;
            toSend.FirstMessage = firstMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          newMessage.SystemMessageState = (int)ESystemMessageState.OneSideIsIn;

          context.SaveChanges();

          if (targetUser.DeviceId != null)
          {
            PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"Someone is in to you!");
          }

          toSend.Type = (int)EMessagesTypesToClient.Ok;
          toSend.Message = newMessage;
          toSend.FirstMessage = firstMessage;
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
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var messageText = jsonObject["text"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var initialMessageIdReal = Convert.ToInt32(initialMessageId);
          var firstMessage = context.FirstMessages.SingleOrDefault(fm => fm.Id == initialMessageIdReal);
          if (firstMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var targetUserId = firstMessage.SourceUserId == userIdParsed ? firstMessage.TargetUserId : firstMessage.SourceUserId;
          var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserId);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            Text = messageText
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            TargetUserId = targetUser.Id,
            Hint = newHint,
            Date = newDate,
            ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
          };

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);
          firstMessage.Message = newMessage;

          // target user does not want source user yet so:
          //  sending target user the message from source user

          context.SaveChanges();

          if (targetUser.DeviceId != null)
          {
            PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"You have a new message!");
          }

          toSend.Type = (int)EMessagesTypesToClient.Ok;
          toSend.Message = newMessage;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    //public string UpdateAskForClue(string userId, string sourcePhoneNumber, string targetUserId, Stream data)
    //{
    //  try
    //  {
    //    dynamic toSend = new ExpandoObject();

    //    var reader = new StreamReader(data);
    //    var text = reader.ReadToEnd();

    //    var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
    //    if (dictionary == null)
    //    {
    //      toSend.Type = EMessagesTypesToClient.Error;
    //      toSend.text = text;
    //      toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
    //      return CommManager.SendMessage(toSend);
    //    }

    //    var firstMessageId = int.Parse(dictionary["firstMessageId"]);

    //    using (var context = new Entities())
    //    {
    //      context.Configuration.ProxyCreationEnabled = false;

    //      // check if userId corresponds to phoneNumber
    //      var userIdParsed = Convert.ToInt32(userId);
    //      var targetUserIdParsed = Convert.ToInt32(targetUserId);
    //      var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
    //      if (sourceUser == null)
    //      {
    //        toSend.Type = EMessagesTypesToClient.Error;
    //        toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
    //        return CommManager.SendMessage(toSend);
    //      }

    //      sourceUser.LastSeen = DateTime.Now.ToString("u");

    //      var newDate = DateTime.UtcNow.ToString("u");
    //      var newHint = new DB.Hint
    //      {
    //        Text = DefaultClue
    //      };
    //      var newMessage = new DB.Message()
    //      {
    //        SourceUserId = userIdParsed,
    //        Hint = newHint,
    //        Date = newDate,
    //        ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
    //      };
    //      var firstMessage = context.FirstMessages.Single(f => f.Id == firstMessageId);

    //      context.Hints.Add(newHint);
    //      context.Messages.Add(newMessage);

    //      firstMessage.Message = newMessage;

    //      // check if target user exists in the system:
    //      //  if so, send him a message telling him a clue is needed
    //      var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserIdParsed);
    //      if (targetUser == null)
    //      {
    //        toSend.Type = EMessagesTypesToClient.Error;
    //        toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
    //        return CommManager.SendMessage(toSend);
    //      }

    //      // target user exists
    //      newMessage.TargetUserId = targetUser.Id;

    //      context.SaveChanges();

    //      if (targetUser.DeviceId != null)
    //      {
    //        PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"Someone needs a clue...");
    //      }

    //      toSend.Type = (int)EMessagesTypesToClient.Ok;
    //      toSend.Message = newMessage;
    //      return CommManager.SendMessage(toSend);
    //    }
    //  }
    //  catch (Exception e)
    //  {
    //    throw new FaultException("Something went wrong. exception is: " + e.Message);
    //  }
    //}

    //public string UpdateIWantUserByChatMessage(string userId, string targetUserId, string firstMessageId,
    //          string hintImageLink, string hintVideoLink, Stream data)
    //{
    //  try
    //  {
    //    dynamic toSend = new ExpandoObject();

    //    var reader = new StreamReader(data);
    //    var text = reader.ReadToEnd();

    //    using (var context = new Entities())
    //    {
    //      context.Configuration.ProxyCreationEnabled = false;

    //      // check if userId corresponds to phoneNumber
    //      var userIdParsed = Convert.ToInt32(userId);
    //      var targetUserIdParsed = Convert.ToInt32(targetUserId);
    //      var firstMessageIdParsed = Convert.ToInt32(firstMessageId);
    //      var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
    //      if (sourceUser == null)
    //      {
    //        toSend.Type = EMessagesTypesToClient.Error;
    //        toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
    //        return CommManager.SendMessage(toSend);
    //      }

    //      var date = DateTime.Now.ToString("u");
    //      sourceUser.LastSeen = date;

    //      var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserIdParsed);
    //      if (targetUser == null)
    //      {
    //        toSend.Type = EMessagesTypesToClient.Error;
    //        toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
    //        return CommManager.SendMessage(toSend);
    //      }

    //      var firstMessage =
    //        context.FirstMessages.SingleOrDefault(
    //          f =>
    //            f.Id == firstMessageIdParsed && f.SourceUserId == userIdParsed && f.TargetUserId == targetUserIdParsed);
    //      if (firstMessage == null)
    //      {
    //        toSend.Type = EMessagesTypesToClient.Error;
    //        toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
    //        return CommManager.SendMessage(toSend);
    //      }

    //      Debug.Assert(firstMessage.MatchFound);

    //      var newHint = new DB.Hint()
    //      {
    //        Text = text,
    //        PictureLink = hintImageLink,
    //        VideoLink = hintVideoLink
    //      };
    //      var newMessage = new DB.Message()
    //      {
    //        Date = date,
    //        SourceUserId = userIdParsed,
    //        TargetUserId = targetUserIdParsed,
    //        Hint = newHint,
    //        ReceivedState = (int)EMessageReceivedState.MessageStateSentToServer
    //      };

    //      context.Hints.Add(newHint);
    //      context.Messages.Add(newMessage);

    //      firstMessage.Message = newMessage;

    //      context.SaveChanges();

    //      if (targetUser.DeviceId != null)
    //      {
    //        PushSharp.PushManager.PushToIos(targetUser.DeviceId, @"You have a new message...");
    //      }

    //      toSend.Type = EMessagesTypesToClient.Ok;
    //      toSend.Message = newMessage;
    //      return CommManager.SendMessage(toSend);
    //    }
    //  }
    //  catch (Exception e)
    //  {
    //    throw new FaultException("Something went wrong. exception is: " + e.Message);
    //  }
    //}
  }
}
