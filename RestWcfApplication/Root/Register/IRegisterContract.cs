using System;
using System.Collections.Generic;
using System.IO;
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
    [WebGet(UriTemplate = "verify?phoneNumber={phoneNumber}&validationCode={validationCode}")]
    string VerifyValidationCode(string phoneNumber, string validationCode);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebGet(UriTemplate = "getUserId?phoneNumber={phoneNumber}")]
    string RegisterViaPhoneNumber(string phoneNumber);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST", 
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare, 
      UriTemplate = "updateUser?userId={userId}&phoneNumber={phoneNumber}&fbUserId={fbUserId}&email={email}")]
    string RegisterUserDetails(string userId, string phoneNumber, string fbUserId, string email, Stream stream);

    /// <summary>
    /// returns "hello" string for testing
    /// </summary>
    /// <returns>"hello"</returns>
    [OperationContract]
    [WebGet(UriTemplate = "hello")]
    string Hello();

    /// <summary>
    /// returns "hello" string for testing
    /// </summary>
    /// <returns>"hello"</returns>
    [OperationContract]
    [WebInvoke(Method = "POST", 
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare, 
      UriTemplate = "hellopush?deviceId={deviceId}")]
    string HelloPush(string deviceId, Stream data);
  }
}
