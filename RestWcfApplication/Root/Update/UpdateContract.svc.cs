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

          user.LastSeen = DateTime.Now.ToString("u");
          context.SaveChanges();

          var result = new List<object>();
          foreach (var firstMessage in context.FirstMessages.Include("Message").Include("SourceUser").Include("TargetUser").Include("Message.Hint"))
          {
            if (firstMessage.SourceUserId == userIdParsed || firstMessage.TargetUserId == userIdParsed)
            {
              if (firstMessage.Id > startingFirstUserMessageIdParsed)
              {
                var otherSideId = firstMessage.SourceUserId == userIdParsed
                  ? firstMessage.TargetUserId
                  : firstMessage.SourceUserId;
                var unreadMessages = context.Messages.Include("Hint").Where(m =>
                  (m.SourceUserId == userIdParsed && m.TargetUserId == otherSideId)
                  || (m.TargetUserId == userIdParsed && m.SourceUserId == otherSideId)).ToList();
                var numberOfUnreadMessages = unreadMessages.Count;
                var anyUnreadSystemMessage =
                  unreadMessages.Any(m => m.SystemMessageState != null && m.SystemMessageState != 0);
                var anyUnreadMessage = unreadMessages.Any(m => string.IsNullOrEmpty(m.Hint.Text) == false);
                result.Add(new {firstMessage, numberOfUnreadMessages, anyUnreadSystemMessage, anyUnreadMessage});
              }
            }
          }

          toSend.Type = EMessagesTypesToClient.MultipleMessages;
          toSend.MultipleMessages = result;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string UpdateFirstUserMessages(string userId, string phoneNumber, string firstUserMessageIds, string lastMessageIds)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        if (string.IsNullOrEmpty(firstUserMessageIds) || string.IsNullOrEmpty(lastMessageIds))
        {
          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }

        phoneNumber = Regex.Replace(phoneNumber, @"[-+ ()]", "");

        var userIdParsed = int.Parse(userId);
        var messagesIdsArray = firstUserMessageIds.Split(new[] {','}).Select(int.Parse).ToList();
        var lastMessageIdsList = lastMessageIds.Split(new[] { ',' }).Select(int.Parse).ToList();

        if (messagesIdsArray.Count() != lastMessageIdsList.Count())
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

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

          user.LastSeen = DateTime.Now.ToString("u");
          context.SaveChanges();

          var result = new List<object>();
          foreach (var firstMessage in context.FirstMessages.Include("Message").Include("Message.Hint"))
          {
            var indexOf = messagesIdsArray.IndexOf(firstMessage.Id);
            if (indexOf != -1)
            {
              var lastMessageId = lastMessageIdsList[indexOf];
              var otherSideId = firstMessage.SourceUserId == userIdParsed ? firstMessage.TargetUserId : firstMessage.SourceUserId;
              var unreadMessages = context.Messages.Include("Hint").Where(m => m.Id > lastMessageId
                                                                       &&
                                                                       (m.SourceUserId == userIdParsed &&
                                                                        m.TargetUserId == otherSideId
                                                                        ||
                                                                        m.TargetUserId == userIdParsed &&
                                                                        m.SourceUserId == otherSideId)).ToList();
              var numberOfUnreadMessages = unreadMessages.Count;
              var anyUnreadSystemMessage = unreadMessages.Any(m => m.SystemMessageState != null && m.SystemMessageState != 0);
              var anyUnreadMessage = unreadMessages.Any(m => string.IsNullOrEmpty(m.Hint.Text) == false);
              result.Add(new { firstMessage, numberOfUnreadMessages, anyUnreadSystemMessage, anyUnreadMessage });
            }
          }

          toSend.Type = EMessagesTypesToClient.MultipleMessages;
          toSend.MultipleMessages = result;
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
            user.LastSeen = DateTime.Now.ToString("u");

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
            user.LastSeen = DateTime.Now.ToString("u");

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
