using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.Want
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IEpisodeContract" in both code and config file together.
  [ServiceContract]
  public interface IWantContract
  {
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare, 
      UriTemplate = "iamin?userId={userId}&token={token}&targetPhoneNumber={targetPhoneNumber}")]
    string UpdateIWantUserByPhoneNumber(string userId, string token, string targetPhoneNumber, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "iaminfacebook?userId={userId}&token={token}&facebookId={facebookId}")]
    string UpdateIWantUserByFacebookId(string userId, string token, string facebookId, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "iaminnewmessage?userId={userId}&token={token}&initialMessageId={initialMessageId}")]
    string UpdateIWantUserExistingMessage(string userId, string token, string initialMessageId, Stream stream);
  }
}
