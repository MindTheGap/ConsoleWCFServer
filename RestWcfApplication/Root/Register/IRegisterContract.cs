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
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "verifyPhoneNumber")]
    string VerifyValidationCode(Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "registerPhoneNumber")]
    string RegisterViaPhoneNumber(Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST", 
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare, 
      UriTemplate = "updateUserFbDetails?userId={userId}")]
    string RegisterUserDetailsFacebookDetails(string userId, Stream stream);

    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "updateUserDeviceId?userId={userId}")]
    string RegisterUserDetailsDeviceId(string userId, Stream stream);

    /// <summary>
    /// returns "hello" string for testing
    /// </summary>
    /// <returns>"hello"</returns>
    [OperationContract]
    [WebGet(UriTemplate = "hello")]
    string Hello();

    /// <summary>
    /// </summary>
    /// <returns></returns>
    [OperationContract]
    [WebGet(UriTemplate = "truncateAll")]
    string TruncateAll();


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
