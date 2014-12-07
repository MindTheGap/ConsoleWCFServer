using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using System.Text.RegularExpressions;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.Update
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class UpdateContract : IUpdateContract
  {
    public string GetNewFirstUserMessages(string userId, string phoneNumber, string startingFirstUserMessageId)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        phoneNumber = Regex.Replace(phoneNumber, @"[-+ ()]", "");

        var userIdParsed = int.Parse(userId);
        var startingFirstUserMessageIdParsed = int.Parse(startingFirstUserMessageId);

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber && u.Id == userIdParsed);
          var user = userList.FirstOrDefault();
          if (user == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.PhoneNumberUserIdMismatch.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          user.LastSeen = DateTime.Now.ToString("g");
          context.SaveChanges();

          var userFirstMessages =
            context.FirstMessages.Where(m => m.Id > startingFirstUserMessageIdParsed 
              && (m.SourceUserId == userIdParsed || m.TargetUserId == userIdParsed))
            .Include("TargetUser").Include("SourceUser").Include("Message").Include("Message.Hint")
              .ToList();

          toSend.Type = EMessagesTypesToClient.MultipleMessages;
          toSend.MultipleMessages = userFirstMessages;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string UpdateFirstUserMessages(string userId, string phoneNumber, string messagesIds)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        phoneNumber = Regex.Replace(phoneNumber, @"[-+ ()]", "");

        var userIdParsed = int.Parse(userId);
        var messagesIdsArray = messagesIds.Split(new[] {','}).Select(int.Parse).ToArray();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber && u.Id == userIdParsed);
          var user = userList.FirstOrDefault();
          if (user == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.PhoneNumberUserIdMismatch.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          user.LastSeen = DateTime.Now.ToString("g");
          context.SaveChanges();

          var userFirstMessages =
            context.FirstMessages.Where(m => messagesIdsArray.Contains(m.Id)
              && (m.SourceUserId == userIdParsed || m.TargetUserId == userIdParsed))
            .Include("TargetUser").Include("SourceUser").Include("Message").Include("Message.Hint")
              .ToList();

          toSend.Type = EMessagesTypesToClient.MultipleMessages;
          toSend.MultipleMessages = userFirstMessages;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string GetUserChatMessages(string sourceUserId, string targetUserId, string startingMessageId)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var sourceUserIdParsed = int.Parse(sourceUserId);
        var targetUserIdParsed = int.Parse(targetUserId);
        var startingMessageIdParsed = startingMessageId == "(null)" ? -1 : int.Parse(startingMessageId);

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var user = context.Users.SingleOrDefault(u => u.Id == sourceUserIdParsed);
          if (user != null)
          {
            user.LastSeen = DateTime.Now.ToString("g");

            context.SaveChanges();
          }

          var userMessages =
            context.Messages.Where(m => m.Id > startingMessageIdParsed 
                                    && (    (m.SourceUserId == sourceUserIdParsed && m.TargetUserId == targetUserIdParsed)
                                        ||  (m.SourceUserId == targetUserIdParsed && m.TargetUserId == sourceUserIdParsed)))
              .Include("SourceUser").Include("TargetUser").Include("Hint")
                                        .ToList();

          toSend.Type = EMessagesTypesToClient.MultipleMessages;
          toSend.MultipleMessages = userMessages;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string GetUserContactsLastSeen(string userId, string phoneNumbersStr)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var contactsArray = phoneNumbersStr.Split(new[] {'.'});
        var sourceUserIdParsed = int.Parse(userId);

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var user = context.Users.SingleOrDefault(u => u.Id == sourceUserIdParsed);
          if (user != null)
          {
            user.LastSeen = DateTime.Now.ToString("g");

            context.SaveChanges();
          }

          var first = true;
          var stringBuilder = new StringBuilder();
          foreach (var contactStr in contactsArray)
          {
            var phoneNumbersArray = contactStr.Split(new[] { ',' });

            if (!first) stringBuilder.Append(",");
            first = false;

            var foundAtLeastOne = false;
            string lastSeen = null;
            foreach (var phoneNumber in phoneNumbersArray)
            {
              var phoneNumberRegex = Regex.Replace(phoneNumber, @"[-+ ()]", "");

              var contact = context.Users.SingleOrDefault(u => u.PhoneNumber == phoneNumberRegex);

              if (contact != null)
              {
                foundAtLeastOne = true;
                lastSeen = contact.LastSeen;
                break;
              }
            }

            stringBuilder.Append(foundAtLeastOne ? lastSeen : "-1");
          }

          toSend.Type = EMessagesTypesToClient.CsvString;
          toSend.CsvString = stringBuilder.ToString();
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }
  }
}
