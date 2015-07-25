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
      UriTemplate = "iamin?userId={userId}&targetPhoneNumber={targetPhoneNumber}")]
    string UpdateIWantUserByPhoneNumber(string userId, string targetPhoneNumber, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare, 
      UriTemplate = "iaminfacebook?userId={userId}&facebookId={facebookId}")]
    string UpdateIWantUserByFacebookId(string userId, string facebookId, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "iaminnewmessage?userId={userId}&initialMessageId={initialMessageId}")]
    string UpdateIWantUserExistingMessage(string userId, string initialMessageId, Stream stream);

    //[OperationContract]
    //[WebInvoke(Method = "POST", UriTemplate = "askforclue?userId={userId}&sourcePhoneNumber={sourcePhoneNumber}&targetUserId={targetUserId}")]
    //string UpdateAskForClue(string userId, string sourcePhoneNumber, string targetUserId, Stream data);

    //[OperationContract]
    //[WebInvoke(Method = "POST", UriTemplate = "match?userId={userId}&targetUserId={targetUserId}&firstMessageId={firstMessageId}&hintImageLink={hintImageLink}&hintVideoLink={hintVideoLink}")]
    //string UpdateIWantUserByChatMessage(string userId, string targetUserId, string firstMessageId,
    //          string hintImageLink, string hintVideoLink, Stream data);
  }
}
