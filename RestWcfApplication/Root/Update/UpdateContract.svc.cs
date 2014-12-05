using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.Update
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class UpdateContract : IUpdateContract
  {
    public string GetUserMessages(string userId, string phoneNumber)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var userIdParsed = int.Parse(userId);

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

          var userFirstMessages =
            context.FirstMessages.Where(m => m.SourceUserId == userIdParsed || m.TargetUserId == userIdParsed)
            .Include("TargetUser").Include("SourceUser").Include("Message")
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

          var userMessages =
            context.Messages.Where(m => m.Id > startingMessageIdParsed 
                                    && (    (m.SourceUserId == sourceUserIdParsed && m.TargetUserId == targetUserIdParsed)
                                        ||  (m.SourceUserId == targetUserIdParsed && m.TargetUserId == sourceUserIdParsed)))
              .Include("SourceUser").Include("TargetUser")
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
  }
}
