using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using System.ServiceModel.Activation;

namespace RestWcfApplication.Root.Want
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class WantContract : IWantContract
  {
    private const string DefaultClue = @"I need another clue...";

    private bool IsStringEmpty(string s)
    {
      return string.IsNullOrEmpty(s) || s == "(null)";
    }

    public string UpdateIWantUserByPhoneNumber(string userId, string sourcePhoneNumber, string targetPhoneNumber,
      string hintImageLink, string hintVideoLink, Stream data)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(data);
        var text = reader.ReadToEnd();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var hintNotUsed = IsStringEmpty(text) && IsStringEmpty(hintImageLink) && IsStringEmpty(hintVideoLink);
          var firstMessage =
            context.FirstMessages.SingleOrDefault(u => ((u.SourceUserId == userIdParsed && u.TargetUser.PhoneNumber == targetPhoneNumber)
                                                    ||  (u.TargetUserId == userIdParsed && u.SourceUser.PhoneNumber == targetPhoneNumber)));

          if (hintNotUsed)
          {
            toSend.DefaultClue = DefaultClue;
          }

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink,
            Text = hintNotUsed ? DefaultClue : text
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            Hint = newHint,
            Date = newDate
          };
          if (firstMessage == null)
          {
            var newFirstMessage = new DB.FirstMessage()
            {
              Date = newDate,
              Message = newMessage,
              SourceUserId = userIdParsed,
              SubjectName = @"No Subject"
            };

            context.FirstMessages.Add(newFirstMessage);

            firstMessage = newFirstMessage;
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
            Twilio.Twilio.SendInvitationMessage(sourceUser.FirstName + " " + sourceUser.LastName, targetPhoneNumber);

            // TODO: check maybe we need to save 3 times to get identity ID field from DB context

            var newTargetUser = new DB.User() { PhoneNumber = targetPhoneNumber };

            newMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            firstMessage.TargetUser = newTargetUser;
            newMessage.SystemMessageState = (int) ESystemMessageState.SentSms;

            context.Users.Add(newTargetUser);

            context.SaveChanges();

            toSend.Type = (int)EMessagesTypesToClient.Message | (int)EMessagesTypesToClient.SystemMessage;
            toSend.SystemMessage = (int)ESystemMessageState.SentSms;
            toSend.Message = newMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user exists
          newMessage.TargetUserId = targetUser.Id;
          firstMessage.TargetUserId = targetUser.Id;

          // checking if target user is in source user also
          var message = context.Messages.SingleOrDefault(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
          if (message != null)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            // TODO: enable a chat between them

            newMessage.SystemMessageState = (int)ESystemMessageState.BothSidesAreIn;
            firstMessage.MatchFound = true;

            context.SaveChanges();

            toSend.Type = (int)EMessagesTypesToClient.Message | (int)EMessagesTypesToClient.SystemMessage;
            toSend.SystemMessage = (int)ESystemMessageState.BothSidesAreIn;
            toSend.Message = newMessage;
            return CommManager.SendMessage(toSend);
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          // TODO: send target user a push notification that source user is in to him

          newMessage.SystemMessageState = (int)ESystemMessageState.OneSideIsIn;

          context.SaveChanges();

          toSend.Type = (int)EMessagesTypesToClient.Message | (int)EMessagesTypesToClient.SystemMessage;
          toSend.SystemMessage = (int)ESystemMessageState.OneSideIsIn;
          toSend.Message = newMessage;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    public string UpdateAskForClue(string userId, string sourcePhoneNumber, string targetUserId, Stream data)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(data);
        var text = reader.ReadToEnd();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var targetUserIdParsed = Convert.ToInt32(targetUserId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("u");

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            Text = DefaultClue
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            Hint = newHint,
            Date = newDate
          };

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          // check if target user exists in the system:
          //  if so, send him a message telling him a clue is needed
          var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserIdParsed);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          // target user exists
          newMessage.TargetUserId = targetUser.Id;

          // TODO: send target user a push notification that source user is in to him

          newMessage.SystemMessageState = (int)ESystemMessageState.ClueNeeded;

          context.SaveChanges();

          toSend.Type = (int)EMessagesTypesToClient.Message | (int)EMessagesTypesToClient.SystemMessage;
          toSend.SystemMessage = (int)ESystemMessageState.ClueNeeded;
          toSend.Message = newMessage;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    public string UpdateIWantUserByUserId(string userId, string targetUserId, string firstMessageId,
              string hintImageLink, string hintVideoLink, Stream data)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(data);
        var text = reader.ReadToEnd();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var targetUserIdParsed = Convert.ToInt32(targetUserId);
          var firstMessageIdParsed = Convert.ToInt32(firstMessageId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var date = DateTime.Now.ToString("u");
          sourceUser.LastSeen = date;

          var targetUser = context.Users.SingleOrDefault(u => u.Id == targetUserIdParsed);
          if (targetUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var firstMessage =
            context.FirstMessages.SingleOrDefault(
              f =>
                f.Id == firstMessageIdParsed && f.SourceUserId == userIdParsed && f.TargetUserId == targetUserIdParsed);
          if (firstMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          Debug.Assert(firstMessage.MatchFound);

          var newHint = new DB.Hint()
          {
            Text = text,
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink
          };
          var newMessage = new DB.Message()
          {
            Date = date,
            SourceUserId = userIdParsed,
            TargetUserId = targetUserIdParsed,
            Hint = newHint
          };

          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          firstMessage.Message = newMessage;

          context.SaveChanges();

          toSend.Type = EMessagesTypesToClient.Message;
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
