using System.Dynamic;
using System.ServiceModel.Activation;
using RestWcfApplication.Communications;
using RestWcfApplication.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace RestWcfApplication.Root.Register
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class RegisterContract : IRegisterContract
  {
    public int RegisterViaPhoneNumber(string phoneNumber)
    {
      try
      {
        using (var context = new Entities())
        {
          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber);
          if (userList.Any())
          {
            return userList.First().Id;
          }

          // TODO: send SMS verification code

          var user = new User() {PhoneNumber = phoneNumber};
          context.Users.Add(user);
          context.SaveChanges();

          return user.Id;
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    public string RegisterUserDetails(string userId, string phoneNumber, string firstName, string lastName, string email)
    {
      try
      {
        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          // check if userId corresponds to phoneNumber
          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == phoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessagesTypesToClient.Error;
            toSend.ErrorInfo = ErrorInfo.SourceUserIdDoesNotExist.ToString("d");
            return toSend;
          }

          var userList = context.Users.Where(u => u.Id == userIdParsed);
          if (userList.Any())
          {
            var user = userList.First();
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
          }

          context.SaveChanges();

          toSend.Type = EMessagesTypesToClient.Ok;
          return toSend;
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    public string Hello()
    {
      const string s = "hello";
      return s;
    }
  }
}
