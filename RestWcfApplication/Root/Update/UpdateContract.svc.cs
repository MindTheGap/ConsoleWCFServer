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
    public string GetAllInitialMessages(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        //var reader = new StreamReader(stream);
        //var text = reader.ReadToEnd();

        //var dictionary = JsonConvert.DeserializeObject<Dictionary<string, int>>(text);
        //if (dictionary == null)
        //{
        //  toSend.Type = EMessagesTypesToClient.Error;
        //  toSend.text = text;
        //  toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
        //  return CommManager.SendMessage(toSend);
        //}

        var userIdParsed = int.Parse(userId);

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userList = context.Users.Where(u => u.Id == userIdParsed);
          var user = userList.FirstOrDefault();
          if (user == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.PhoneNumberUserIdMismatch.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          user.LastSeen = DateTime.Now.ToString("u");

          var result = new List<object>();
          foreach (var initialMessage in context.FirstMessages.Include("Message").Include("SourceUser").Include("TargetUser").Include("Message.Hint")
            .Where(m => m.SourceUserId == userIdParsed || m.TargetUserId == userIdParsed))
          {
            var otherSideId = initialMessage.SourceUserId == userIdParsed
              ? initialMessage.TargetUserId
              : initialMessage.SourceUserId;
            var unreadMessages = context.Messages.Include("SourceUser").Include("TargetUser").Include("Hint")
              .Where(m => 
                (m.ReceivedState == (int)EMessageReceivedState.MessageStateSentToServer
                && m.TargetUserId == userIdParsed)
                || (m.ReceivedState == (int)EMessageReceivedState.MessageStateReadByClient
                && m.SourceUserId == userIdParsed)).ToList();
            if (unreadMessages.Count > 0)
            {
              unreadMessages.ForEach(m =>
              {
                if (m.ReceivedState == (int) EMessageReceivedState.MessageStateSentToServer)
                {
                  m.ReceivedState = (int) EMessageReceivedState.MessageStateSentToClient;
                }
                else // == MessageStateReadByClient
                {
                  m.ReceivedState = (int)EMessageReceivedState.MessageStateReadByClientAck;
                }
              });
              result.Add(new { InitialMessage = initialMessage, UnreadMessages = unreadMessages });
            }
          }

          context.SaveChanges();

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

    public string GetUserChatMessages(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, int>>(text);
        if (dictionary == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var targetUserId = dictionary["targetUserId"];
        var maxChatMessageId = dictionary["maxChatMessageId"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var user = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (user != null)
          {
            user.LastSeen = DateTime.Now.ToString("u");
          }

          var userMessages =
            context.Messages.Where(m => m.Id > maxChatMessageId
                                    && ((m.SourceUserId == userIdParsed && m.TargetUserId == targetUserId)
                                        || (m.SourceUserId == targetUserId && m.TargetUserId == userIdParsed)))
              .Include("SourceUser").Include("TargetUser").Include("Hint");

          foreach (var userMessage in userMessages)
          {
            if (userIdParsed == userMessage.TargetUserId && userMessage.ReceivedState == (int)EMessageReceivedState.MessageStateSentToServer)
            {
              userMessage.ReceivedState = (int)EMessageReceivedState.MessageStateSentToClient;
            }
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

    public string ReadUserChatMessages(string userId, Stream stream)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();

        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, int>>(text);
        if (dictionary == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.text = text;
          toSend.ErrorInfo = ErrorInfo.BadArgumentsLength.ToString("d");
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var initialMessageId = dictionary["initialMessageId"];
        var maxChatMessageId = dictionary["maxChatMessageId"];

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var user = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (user != null)
          {
            user.LastSeen = DateTime.Now.ToString("u");
          }

          var initialMessage = context.FirstMessages.SingleOrDefault(m => m.Id == initialMessageId);
          if (initialMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.UserIdDoesNotExist.ToString("d");
            return CommManager.SendMessage(toSend);
          }

          var realTargetUserId = initialMessage.SourceUserId == userIdParsed
            ? initialMessage.TargetUserId
            : initialMessage.SourceUserId;
          var realTargetUser = context.Users.FirstOrDefault(u => u.Id == realTargetUserId);

          var sendPush = false;
          var userMessages =
            context.Messages.Where(m => m.ReceivedState < (int)EMessageReceivedState.MessageStateReadByClient
              && m.Id <= maxChatMessageId
              && m.TargetUserId == userIdParsed && m.SourceUserId == realTargetUserId);
          foreach (var userMessage in userMessages)
          {
            userMessage.ReceivedState = (int)EMessageReceivedState.MessageStateReadByClient;
            sendPush = true;
          }

          context.SaveChanges();

          if (sendPush && realTargetUser.DeviceId != null)
          {
            PushManager.PushToIos(realTargetUser.DeviceId); // send empty push for client to only update the UI and not get an actual message
          }

          toSend.Type = EMessagesTypesToClient.Ok;
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
