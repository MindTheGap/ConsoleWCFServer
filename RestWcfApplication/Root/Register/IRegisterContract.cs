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
    /// <param name="email"></param>
    /// <returns>userId</returns>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "getUserId?email={email}")]
    int RegisterViaEmail(string email);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns>userId</returns>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "getUserId?phoneNumber={phoneNumber}")]
    int RegisterViaPhoneNumber(string phoneNumber);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="firstName"></param>
    /// <param name="lastName"></param>
    /// <param name="email"></param>
    /// <returns>userId</returns>
    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "updateUser?userId={userId}&firstName={firstName}&lastName={lastName}&email={email}")]
    void RegisterUserDetails(string userId, string firstName, string lastName, string email);

    /// <summary>
    /// returns "hello" string for testing
    /// </summary>
    /// <returns>"hello"</returns>
    [OperationContract]
    [WebInvoke(Method = "GET")]
    string Hello();
  }
}
