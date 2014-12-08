using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
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
    private const string DefaultClue = @"Guess who...";

    private bool IsStringEmpty(string s)
    {
      return string.IsNullOrEmpty(s) || s == "(null)";
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

          sourcePhoneNumber = Regex.Replace(sourcePhoneNumber, @"[-+ ()]", "");
          targetPhoneNumber = Regex.Replace(targetPhoneNumber, @"[-+ ()]", "");

          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          sourceUser.LastSeen = DateTime.Now.ToString("g");

          var hintNotUsed = IsStringEmpty(hint) && IsStringEmpty(hintImageLink) && IsStringEmpty(hintVideoLink);
          var firstMessage =
            context.FirstMessages.SingleOrDefault(u => ((u.SourceUserId == userIdParsed && u.TargetUser.PhoneNumber == targetPhoneNumber)
                                                    ||  (u.TargetUserId == userIdParsed && u.SourceUser.PhoneNumber == targetPhoneNumber)));
          DB.FirstMessage newFirstMessage = null;

          if (hintNotUsed)
          {
            toSend.DefaultClue = DefaultClue;
          }

          var newDate = DateTime.UtcNow.ToString("u");
          var newHint = new DB.Hint
          {
            PictureLink = hintImageLink,
            VideoLink = hintVideoLink,
            Text = hintNotUsed ? DefaultClue : hint
          };
          var newMessage = new DB.Message()
          {
            SourceUserId = userIdParsed,
            Hint = newHint,
            Date = newDate
          };
          if (firstMessage == null)
          {
            newFirstMessage = new DB.FirstMessage()
            {
              Date = newDate,
              Message = newMessage,
              SourceUserId = userIdParsed,
              SubjectName = @"No Subject"
            };

            context.FirstMessages.Add(newFirstMessage);
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
            // TODO: Send him an SMS

            // TODO: check maybe we need to save 3 times to get identity ID field from DB context

            var newTargetUser = new DB.User() { PhoneNumber = targetPhoneNumber };

            newMessage.TargetUser = newMessage.TargetUser = newTargetUser;
            if (newFirstMessage != null)
            {
              newFirstMessage.TargetUser = newTargetUser;
            }
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
          if (newFirstMessage != null)
          {
            newFirstMessage.TargetUserId = targetUser.Id;
          }

          // checking if target user is in source user also
          var message = context.Messages.SingleOrDefault(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
          if (message != null)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            // TODO: enable a chat between them

            newMessage.SystemMessageState = (int)ESystemMessageState.BothSidesAreIn;

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
  }
}
