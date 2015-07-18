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
  public class InitialMessageWithUnreadMessages
  {
    public FirstMessage InitialMessage { get; set; }
    public List<Message> UnreadMessages { get; set; }
  };
    

  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class UpdateContract : IUpdateContract
  {
    public string GetAllInitialMessages(string userId, Stream stream)
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
          toSend.Error = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var startingNotificationId = dictionary.ContainsKey("startingNotificationId")
          ? dictionary["startingNotificationId"]
          : -1;

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userList = context.Users.Where(u => u.Id == userIdParsed);
          var user = userList.FirstOrDefault();
          if (user == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.FatalError = "true";
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          user.LastSeen = DateTime.Now.ToString("u");

          var result = new List<InitialMessageWithUnreadMessages>();
          foreach (var initialMessage in context.FirstMessages.Include("Messages").Include("SourceUser").Include("TargetUser")
            .Where(m => m.SourceUserId == userIdParsed || m.TargetUserId == userIdParsed))
          {
            var startingMessageId = dictionary.ContainsKey(initialMessage.Id.ToString("D")) ? dictionary[initialMessage.Id.ToString("D")] : -1;
            var unreadMessages = context.Messages.Include("SourceUser").Include("TargetUser").Include("Hint")
              .Where(m => m.Id > startingMessageId && m.FirstMessageId == initialMessage.Id).ToList();

            var initialMessageResult = new InitialMessageWithUnreadMessages()
            {
              InitialMessage = initialMessage
            };

            if (unreadMessages.Count > 0)
            {
              var neededUnreadMessages = new List<Message>();
              unreadMessages.ForEach(m =>
              {
                if (m.ReceivedState != (int) EMessageReceivedState.MessageStateReadByClientAck
                    && ((m.SystemMessageState != null && m.SourceUserId == userIdParsed)
                        || (m.SystemMessageState == null)))
                {
                  neededUnreadMessages.Add(m);
                }

                if (m.ReceivedState == (int) EMessageReceivedState.MessageStateSentToServer)
                {
                  m.ReceivedState = (int) EMessageReceivedState.MessageStateSentToClient;
                }
                else if (m.ReceivedState == (int) EMessageReceivedState.MessageStateReadByClient)
                {
                  m.ReceivedState = (int) EMessageReceivedState.MessageStateReadByClientAck;
                }
              });

              initialMessageResult.UnreadMessages = neededUnreadMessages;
            }

            result.Add(initialMessageResult);
          }

          var notifications = context.Notifications.Where(n => n.UserId == userIdParsed && n.Id > startingNotificationId).ToList();
          ;

          context.SaveChanges();

          GC.Collect();

          toSend.Type = EMessagesTypesToClient.MultipleMessages;
          toSend.MultipleMessages = result;
          toSend.Notifications = notifications;
            
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
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var initialMessageId = dictionary["initialMessageId"];
        var maxChatMessageId = dictionary.ContainsKey("maxChatMessageId") ? dictionary["maxChatMessageId"] : -1;
        var chatMessageId = dictionary.ContainsKey("chatMessageId") ? dictionary["chatMessageId"] : -1;

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var user = context.Users.SingleOrDefault(u => u.Id == userIdParsed);
          if (user == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          user.LastSeen = DateTime.Now.ToString("u");

          var initialMessage = context.FirstMessages.SingleOrDefault(m => m.Id == initialMessageId);
          if (initialMessage == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
            return CommManager.SendMessage(toSend);
          }

          var realTargetUserId = initialMessage.SourceUserId == userIdParsed
            ? initialMessage.TargetUserId
            : initialMessage.SourceUserId;
          var realTargetUser = context.Users.FirstOrDefault(u => u.Id == realTargetUserId);

          var sendPush = false;
          var userMessages =
            context.Messages.Include("Hint").Where(m => m.ReceivedState < (int)EMessageReceivedState.MessageStateReadByClient
              && ((maxChatMessageId > -1 && m.Id <= maxChatMessageId) || (chatMessageId > -1 && m.Id == chatMessageId))
              && m.TargetUserId == userIdParsed && m.SourceUserId == realTargetUserId);
          foreach (var userMessage in userMessages)
          {
            // check for text message or it's a specific media message
            if (chatMessageId > -1 || (userMessage.Hint.PictureLink == null && userMessage.Hint.VideoLink == null))
            {
              userMessage.ReceivedState = (int)EMessageReceivedState.MessageStateReadByClient;
              sendPush = true;
            }
          }

          context.SaveChanges();

          if (sendPush && realTargetUser != null && realTargetUser.DeviceId != null)
          {
            var userName = user.FirstName != null ? user.FirstName + " " + user.LastName : user.DisplayName;
            PushManager.PushToIos(realTargetUser.DeviceId, string.Format("{0} just read your message!", userName));
          }

          GC.Collect();

          toSend.Type = EMessagesTypesToClient.Ok;
          return CommManager.SendMessage(toSend);
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }

    public string GetUserContactsLastSeenAndProfileImageLinks(string userId, Stream stream)
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
          toSend.ErrorInfo = ErrorDetails.BadArguments;
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

          var resultList = new Dictionary<string,List<string>>();
          foreach (var contactPhoneNumberList in contactsList)
          {
            foreach (var phoneNumber in contactPhoneNumberList)
            {
              var contact = context.Users.SingleOrDefault(u => u.PhoneNumber == phoneNumber);

              if (contact != null)
              {
                resultList[phoneNumber] = new List<string>() {contact.LastSeen,contact.ProfileImageLink};
                break;
              }
            }
          }

          GC.Collect();

          toSend.Type = EMessagesTypesToClient.Ok;
          toSend.MultipleMessages = resultList;
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
