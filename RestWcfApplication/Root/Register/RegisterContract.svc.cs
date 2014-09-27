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
    public int Register(string userEmail)
    {
      try
      {
        using (var context = new Entities())
        {
          var user = new User() {Email = userEmail};
          context.Users.Add(user);
          context.SaveChanges();

          return 0;
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }
  }
}
