using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.Token
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class TokenService : ITokenService
  {
    #region Public Service Functions

    public string GenerateTokenByEmail(string email, string password)
    {
      try
      {
        using (var context = new Entities())
        {
          var userList = context.Users.Where(u => u.Email == email);
          if (!userList.Any())
          {
            throw new FaultException("no such user exists with that email");
          }

          return GenerateToken_aux(context, userList);
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    public string GenerateTokenByPhone(string phoneNumber, string password)
    {
      try
      {
        using (var context = new Entities())
        {
          var userList = context.Users.Where(u => u.PhoneNumber == phoneNumber);
          if (!userList.Any())
          {
            throw new FaultException("no such user exists with that phone number");
          }

          return GenerateToken_aux(context, userList);
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    #endregion

    #region Static Functions

    public static bool IsTokenValid(string token, string userId)
    {
      try
      {
        int userIdParsed;
        if (int.TryParse(userId, out userIdParsed))
        {
          using (var context = new Entities())
          {
            var tokenList = context.Tokens.Where(t => t.UserId == userIdParsed && t.Code == token);
            if (tokenList.Any())
            {
              var data = Convert.FromBase64String(token);
              var when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
              if (when < DateTime.UtcNow.AddHours(-24)) // TODO: move this hard-coded value to XML file
              {
                throw new FaultException("Token too old");
              }

              return true;
            }
          }
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }

      return false;
    }

    #endregion

    #region Private Functions

    private static string GenerateToken_aux(Entities context, IQueryable<User> userList)
    {
      var time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
      var key = Guid.NewGuid().ToByteArray();
      var token = Convert.ToBase64String(time.Concat(key).ToArray());

      var tokenDbList = context.Tokens.Where(t => t.UserId == userList.First().Id).ToList();
      if (tokenDbList.Any())
      {
        context.Tokens.Remove(tokenDbList.First());
      }
      else
      {
        context.Tokens.Add(new DB.Token {UserId = userList.First().Id, Code = token});
      }

      context.SaveChanges();
      return token;
    }

    #endregion
  }
}
