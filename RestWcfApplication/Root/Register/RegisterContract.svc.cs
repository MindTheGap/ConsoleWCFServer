using System.ServiceModel.Activation;
using RestWcfApplication.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace RestWcfApplication.Root.Register
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "RegisterContract" in code, svc and config file together.
  // NOTE: In order to launch WCF Test Client for testing this service, please select RegisterContract.svc or RegisterContract.svc.cs at the Solution Explorer and start debugging.
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class RegisterContract : IRegisterContract
  {
    public int RegisterViaEmail(string email)
    {
      try
      {
        using (var context = new Entities())
        {
          var userList = context.Users.Where(u => u.Email == email);
          if (userList.Any())
          {
            return userList.First().Id;
          }

          var user = new User() {Email = email};
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

    public void RegisterUserDetails(string userId, string firstName, string lastName, string email)
    {
      try
      {
        int userIdParsed;
        if (!int.TryParse(userId, out userIdParsed)) return;

        using (var context = new Entities())
        {
          var userList = context.Users.Where(u => u.Id == userIdParsed);
          if (userList.Any())
          {
            var user = userList.First();
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
          }

          context.SaveChanges();
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
