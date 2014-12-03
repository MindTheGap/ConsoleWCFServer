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
    /// verifies the validation code to get the user validated.
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "verify?phoneNumber={phoneNumber}&validationCode={validationCode}")]
    string VerifyValidationCode(string phoneNumber, string validationCode);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "GET", UriTemplate = "getUserId?phoneNumber={phoneNumber}")]
    string RegisterViaPhoneNumber(string phoneNumber);

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
