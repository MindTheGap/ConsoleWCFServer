using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp.Extensions;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using RestWcfApplication.PushSharp;
using Message = RestWcfApplication.DB.Message;

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
                result.Add(new {FirstMessage = firstMessage, numberOfUnreadMessages, anyUnreadSystemMessage, anyUnreadMessage});
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

    public string UpdateFirstUserMessages(string userId, string phoneNumber, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, List<String>>>(text);
        if (dictionary == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var messagesIdsArrayStr = dictionary["firstUserMessageIds"];
        var lastMessageIdsListStr = dictionary["lastMessageIds"];
        if (messagesIdsArrayStr == null || lastMessageIdsListStr == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorInfo.BadVerificationCode.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var messagesIdsArray = messagesIdsArrayStr.Select(int.Parse).ToList();
        var lastMessageIdsList = lastMessageIdsListStr.Select(int.Parse).ToList();

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
          foreach (var firstMessage in context.FirstMessages.Include("Message").Include("Message.Hint").Include("SourceUser").Include("TargetUser"))
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
              result.Add(new { FirstMessage = firstMessage, numberOfUnreadMessages, anyUnreadSystemMessage, anyUnreadMessage });
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
          }

          var firstMessage =
            context.FirstMessages.Where(
              f => (f.SourceUserId == sourceUserIdParsed && f.TargetUserId == targetUserIdParsed)
                   || (f.SourceUserId == targetUserIdParsed && f.TargetUserId == sourceUserIdParsed)).Include("SourceUser").Include("TargetUser").SingleOrDefault();
          if (firstMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var realSourceUser = firstMessage.SourceUser;
          var realTargetUser = firstMessage.TargetUser;

          var userMessages =
            context.Messages.Where(m => m.Id > startingMessageIdParsed 
                                    && (    (m.SourceUserId == sourceUserIdParsed && m.TargetUserId == targetUserIdParsed)
                                        ||  (m.SourceUserId == targetUserIdParsed && m.TargetUserId == sourceUserIdParsed)))
              .Include("SourceUser").Include("TargetUser").Include("Hint");

          foreach (var userMessage in userMessages)
          {
            if (sourceUserIdParsed == realTargetUser.Id && userMessage.ReceivedState == (int)EMessageReceivedState.MessageStateSentToServer)
            {
              userMessage.ReceivedState = (int)EMessageReceivedState.MessageStateSendToClient;  
            }
          }

          if (sourceUserIdParsed == realTargetUser.Id && realSourceUser.DeviceId != null)
          {
            PushManager.PushToIos(realSourceUser.DeviceId); // send empty push for client to only update the UI and not get an actual message
          }

          context.SaveChanges();

          toSend.Type = EMessagesTypesToClient.Ok;
          toSend.Messages = userMessages;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string GetUserContactsLastSeen(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var contactsList = JsonConvert.DeserializeObject<List<List<string>>>(text);
        if (contactsList == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

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

          var resultList = new List<string>();
          foreach (var contactPhoneNumberList in contactsList)
          {
            var foundContact = false;
            foreach (var phoneNumber in contactPhoneNumberList)
            {
              var contact = context.Users.SingleOrDefault(u => u.PhoneNumber == phoneNumber);

              if (contact != null)
              {
                resultList.Add(contact.LastSeen);
                foundContact = true;
                break;
              }
            }

            if (!foundContact)
            {
              resultList.Add("0");
            }
          }

          toSend.Type = EMessagesTypesToClient.StringsList;
          toSend.StringsList = resultList;
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
