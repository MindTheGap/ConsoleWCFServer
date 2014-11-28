using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using System.ServiceModel.Activation;
using RestWcfApplication.Root.Token;
using RestWcfApplication.Root.Want;

namespace RestWcfApplication.Root.Want
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class WantContract : IWantContract
  {
    public string UpdateIWantUserByEmail(string userId, string sourcePhoneNumber, string emailAddress,
      string hint, string hintImageLink, string hintVideoLink)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.SourceUserIdDoesNotExist.ToString("d");
            return toSend;
          }

          var newDate = DateTime.UtcNow.ToString("u");
          var newSystemMessage = new DB.SystemMessage()
          {
            Date = newDate,
            SourceUserId = userIdParsed
          };
          var newHint = new DB.Hint()
          {
            Text = hint,
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink
          };
          var newMessage = new DB.Message()
          {
            SourceUser = sourceUser,
            Hint = newHint,
            Date = newDate
          };

          context.SystemMessages.Add(newSystemMessage);
          context.Hints.Add(newHint);
          context.Messages.Add(newMessage);

          // check if target user exists in the system:
          //  if not, send him an email
          //  if so, check if he's also in the source user:
          //    if not, send him a message telling him someone (source user) is in him
          //    if so, love is in the air - connect chat between them
          var targetUser = context.Users.SingleOrDefault(u => u.Email == emailAddress);
          if (targetUser == null)
          {
            // target user doesn't exist in the system yet
            // TODO: Send him an SMS

            // TODO: check maybe we need to save 3 times to get identity ID field from DB context

            var newTargetUser = new DB.User() { Email = emailAddress };

            newSystemMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            newSystemMessage.MessageState = (int)ESystemMessageState.SentSms;

            context.Users.Add(newTargetUser);

            context.SaveChanges();

            toSend.Type = EMessagesTypesToClient.SystemMessage;
            toSend.SystemMessage = newSystemMessage;
            return toSend;
          }

          // target user exists
          newMessage.TargetUser = newSystemMessage.TargetUser = targetUser;

          // checking if target user is in source user also
          var message = context.Messages.SingleOrDefault(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
          if (message != null)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            // TODO: enable a chat between them

            newSystemMessage.MessageState = (int)ESystemMessageState.BothSidesAreIn;

            context.SaveChanges();

            toSend.Type = EMessagesTypesToClient.MatchFound;
            toSend.SystemMessage = newSystemMessage;
            toSend.Message = newMessage;
            return toSend;
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          // TODO: send target user a push notification that source user is in to him

          newSystemMessage.MessageState = (int)ESystemMessageState.OneSideIsIn;

          toSend.Type = EMessagesTypesToClient.SystemMessage;
          toSend.SystemMessage = ESystemMessageState.OneSideIsIn;
          return toSend;
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }

    public string UpdateIWantUserByPhoneNumber(string userId, string sourcePhoneNumber, string targetPhoneNumber,
      string hint, string hintImageLink, string hintVideoLink)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.SourceUserIdDoesNotExist.ToString("d");
            return toSend;
          }

          var newDate = DateTime.UtcNow.ToString("u");
          var newSystemMessage = new DB.SystemMessage()
          {
            Date = newDate,
            SourceUserId = userIdParsed
          };
          var newHint = new DB.Hint()
          {
            Text = hint,
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink
          };
          var newMessage = new DB.Message()
          {
            SourceUser = sourceUser,
            Hint = newHint,
            Date = newDate
          };

          context.SystemMessages.Add(newSystemMessage);
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
            // TODO: Send him an SMS

            // TODO: check maybe we need to save 3 times to get identity ID field from DB context

            var newTargetUser = new DB.User() { PhoneNumber = targetPhoneNumber };

            newSystemMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            newSystemMessage.MessageState = (int) ESystemMessageState.SentSms;

            context.Users.Add(newTargetUser);

            context.SaveChanges();

            toSend.Type = EMessagesTypesToClient.SystemMessage;
            toSend.SystemMessage = newSystemMessage;
            return toSend;
          }

          // target user exists
          newMessage.TargetUser = newSystemMessage.TargetUser = targetUser;

          // checking if target user is in source user also
          var message = context.Messages.SingleOrDefault(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
          if (message != null)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            // TODO: enable a chat between them

            newSystemMessage.MessageState = (int) ESystemMessageState.BothSidesAreIn;

            context.SaveChanges();

            toSend.Type = EMessagesTypesToClient.MatchFound;
            toSend.SystemMessage = newSystemMessage;
            toSend.Message = newMessage;
            return toSend;
          }

          // target user does not want source user yet so:
          //  1. sending source user system message to let him know
          //  2. sending target user that source user is in to him

          // TODO: send target user a push notification that source user is in to him

          newSystemMessage.MessageState = (int) ESystemMessageState.OneSideIsIn;

          toSend.Type = EMessagesTypesToClient.SystemMessage;
          toSend.SystemMessage = ESystemMessageState.OneSideIsIn;
          return toSend;
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
      }
    }
  }
}
