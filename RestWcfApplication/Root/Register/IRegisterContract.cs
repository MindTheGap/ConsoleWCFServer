using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RestWcfApplication.Root.Register
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IRegisterContract" in both code and config file together.
  [ServiceContract]
  public interface IRegisterContract
  {
    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    /// <param name="phoneNumber"></param>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "getUserId?phoneNumber={phoneNumber}")]
    int RegisterViaPhoneNumber(string phoneNumber);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="email"></param>
    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "updateUser?userId={userId}&firstName={firstName}&lastName={lastName}&email={email}")]
    string RegisterUserDetails(string userId, string phoneNumber, string firstName, string lastName, string email);

    /// <summary>
    /// returns "hello" string for testing
    /// </summary>
    /// <returns>"hello"</returns>
    [OperationContract]
    [WebInvoke(Method = "GET")]
    string Hello();
  }
}
