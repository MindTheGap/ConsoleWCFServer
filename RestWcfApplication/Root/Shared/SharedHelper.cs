using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using Newtonsoft.Json;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.Shared
{
  public static class SharedHelper
  {
    public static T QueryForObject<T>(string propertyName, Func<T, bool> predicate) where T : class
    {
      using (var context = new Entities())
      {
        context.Configuration.ProxyCreationEnabled = false;

        var dbSet = context.GetType().GetProperty(propertyName).GetValue(context) as IEnumerable<T>;
        return dbSet.SingleOrDefault(predicate);
      }
    }

    public static bool DeserializeObject<T>(Stream stream, out T deserializedObject, out dynamic toSend) where T : class
    {
      toSend = new ExpandoObject();

      var reader = new StreamReader(stream);
      var text = reader.ReadToEnd();

      deserializedObject = JsonConvert.DeserializeObject<T>(text);
      if (deserializedObject == null)
      {
        toSend.Type = EMessagesTypesToClient.Error;
        toSend.text = text;
        toSend.ErrorInfo = ErrorDetails.BadArguments;
        return false;
      }

      return true;
    }

    public static bool DeserializeObjectAndUpdateLastSeen<T>(string userId, Stream stream, out T deserializedObject, out User sourceUser, out dynamic toSend) where T : class
    {
      if (!DeserializeObject(stream, out deserializedObject, out toSend))
      {
        sourceUser = null;
        return false;
      }

      var sourceUserIdParsed = int.Parse(userId);

      using (var context = new Entities())
      {
        context.Configuration.ProxyCreationEnabled = false;

        sourceUser = context.Users.SingleOrDefault(u => u.Id == sourceUserIdParsed);
        if (sourceUser == null)
        {
          toSend.Type = EMessagesTypesToClient.Error;
          toSend.ErrorInfo = ErrorDetails.UserIdDoesNotExist;
          return false;
        }

        sourceUser.LastSeen = DateTime.Now.ToString("u");

        context.SaveChanges();
      }

      return true;
    }
  }
}