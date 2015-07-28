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
using RestWcfApplication.Root.Shared;
using Message = RestWcfApplication.DB.Message;

namespace RestWcfApplication.Root.Update
{
  public class InitialMessageWithUnreadMessages
  {
    public FirstMessage InitialMessage { get; set; }
    public List<Message> UnreadMessages { get; set; }
  };
    

  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
                   ConcurrencyMode = ConcurrencyMode.Multiple)]
  public class UpdateContract : IUpdateContract
  {
    public string GetAllInitialMessages(string userId, Stream stream)
    {
      try
      {
        Dictionary<string, int> dictionary;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out dictionary, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var startingNotificationId = dictionary.ContainsKey("startingNotificationId")
          ? dictionary["startingNotificationId"]
          : -1;

        List<InitialMessageWithUnreadMessages> result;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          result = new List<InitialMessageWithUnreadMessages>();
          foreach (
            var initialMessage in context.FirstMessages
              .Where(m => m.SourceUserId == userIdParsed || m.TargetUserId == userIdParsed))
          {
            var startingMessageId = dictionary.ContainsKey(initialMessage.Id.ToString("D"))
              ? dictionary[initialMessage.Id.ToString("D")]
              : -1;
            var unreadMessages = context.Messages.Include("Hint")
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

          context.SaveChanges();
        }

        List<Notification> notifications;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;
          
          notifications = context.Notifications.Where(n => n.UserId == userIdParsed && n.Id > startingNotificationId).ToList();
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          foreach (var initialMessageWithUnreadMessages in result)
          {
            var initialMessageSourceUser =
              context.Users.First(u => u.Id == initialMessageWithUnreadMessages.InitialMessage.SourceUserId);
            var targetUser =
              context.Users.First(u => u.Id == initialMessageWithUnreadMessages.InitialMessage.TargetUserId);

            initialMessageWithUnreadMessages.InitialMessage.TargetUser = targetUser;
            initialMessageWithUnreadMessages.InitialMessage.SourceUser = initialMessageSourceUser;
          }
        }

        toSend.Type = EMessagesTypesToClient.MultipleMessages;
        toSend.MultipleMessages = result;
        toSend.Notifications = notifications;

        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string ReadUserChatMessages(string userId, Stream stream)
    {
      try
      {
        Dictionary<string, int> dictionary;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out dictionary, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        var userIdParsed = int.Parse(userId);
        var initialMessageId = dictionary["initialMessageId"];
        var maxChatMessageId = dictionary.ContainsKey("maxChatMessageId") ? dictionary["maxChatMessageId"] : -1;
        var chatMessageId = dictionary.ContainsKey("chatMessageId") ? dictionary["chatMessageId"] : -1;

        var initialMessage = SharedHelper.QueryForObject<FirstMessage>("FirstMessages", m => m.Id == initialMessageId);
        if (initialMessage == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        var realTargetUserId = initialMessage.SourceUserId == userIdParsed
          ? initialMessage.TargetUserId
          : initialMessage.SourceUserId;
        var realTargetUser = SharedHelper.QueryForObject<User>("Users", u => u.Id == realTargetUserId);
        if (realTargetUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.BadArguments;
          return CommManager.SendMessage(toSend);
        }

        bool sendPush = false;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          context.Users.Attach(realTargetUser);
          context.FirstMessages.Attach(initialMessage);

          var userMessages =
            context.Messages.Include("Hint")
              .Where(m => m.ReceivedState < (int) EMessageReceivedState.MessageStateReadByClient
                          &&
                          ((maxChatMessageId > -1 && m.Id <= maxChatMessageId) ||
                           (chatMessageId > -1 && m.Id == chatMessageId))
                          && m.TargetUserId == userIdParsed && m.SourceUserId == realTargetUserId);
          foreach (var userMessage in userMessages)
          {
            // check for text message or it's a specific media message
            if (chatMessageId > -1 || (userMessage.Hint.PictureLink == null && userMessage.Hint.VideoLink == null))
            {
              userMessage.ReceivedState = (int) EMessageReceivedState.MessageStateReadByClient;
              sendPush = true;
            }
          }

          context.SaveChanges();
        }

        if (sendPush && realTargetUser.DeviceId != null)
        {
          var userName = sourceUser.FirstName != null ? sourceUser.FirstName + " " + sourceUser.LastName : sourceUser.DisplayName;
          PushManager.PushToIos(realTargetUser.DeviceId, string.Format("{0} just read your message!", userName));
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }

    public string GetUserContactsLastSeenAndProfileImageLinks(string userId, Stream stream)
    {
      try
      {
        List<List<string>> contactsList;
        User sourceUser;
        dynamic toSend;
        if (!SharedHelper.DeserializeObjectAndUpdateLastSeen(userId, stream, out contactsList, out sourceUser, out toSend))
        {
          return CommManager.SendMessage(toSend);
        }

        Dictionary<string, List<string>> resultList = null;
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          resultList = new Dictionary<string, List<string>>();
          foreach (var contactPhoneNumberList in contactsList)
          {
            foreach (var phoneNumber in contactPhoneNumberList)
            {
              var contact = context.Users.SingleOrDefault(u => u.PhoneNumber == phoneNumber);

              if (contact != null)
              {
                resultList[phoneNumber] = new List<string>() {contact.LastSeen, contact.ProfileImageLink};
                break;
              }
            }
          }
        }

        toSend.Type = EMessagesTypesToClient.Ok;
        toSend.MultipleMessages = resultList;
        return CommManager.SendMessage(toSend);
      }
      catch (Exception e)
      {
        dynamic toSend = new ExpandoObject();
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.ErrorInfo = e.Message;
        toSend.InnerMessage = e.InnerException;
        return CommManager.SendMessage(toSend);
      }
    }
  }
}
