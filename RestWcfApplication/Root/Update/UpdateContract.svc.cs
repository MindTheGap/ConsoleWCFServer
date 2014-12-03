using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.Update
{
  public class UpdateContract : IUpdateContract
  {
    public string UpdateUserMessages(string userId, string phoneNumber)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        var userIdParsed = int.Parse(userId);

        using (var context = new Entities())
        {
          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber && u.Id == userIdParsed);

          toSend.Type = EMessagesTypesToClient.MultipleMessages;

          if (!userList.Any())
          {
            return toSend;
          }

          var user = userList.First();
          var messages = user.MessagesAsSourceUser.ToList();
          messages.AddRange(user.MessagesAsTargetUser.ToList());
          toSend.MultipleMessages = messages;
          return toSend;
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message + ". InnerException: " + e.InnerException);
      }
    }
  }
}
