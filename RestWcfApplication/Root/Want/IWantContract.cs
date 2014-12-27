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
    [WebInvoke(Method = "POST", UriTemplate = "iamin?userId={userId}&sourcePhoneNumber={sourcePhoneNumber}&targetPhoneNumber={targetPhoneNumber}&hintImageLink={hintImageLink}&hintVideoLink={hintVideoLink}")]
    string UpdateIWantUserByPhoneNumber(string userId, string sourcePhoneNumber, string targetPhoneNumber,
              string hintImageLink, string hintVideoLink, Stream data);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare, 
      UriTemplate = "iaminfacebook?userId={userId}&facebookId={facebookId}")]
    string UpdateIWantUserByFacebookId(string userId, string facebookId, Stream data);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "askforclue?userId={userId}&sourcePhoneNumber={sourcePhoneNumber}&targetUserId={targetUserId}")]
    string UpdateAskForClue(string userId, string sourcePhoneNumber, string targetUserId, Stream data);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "match?userId={userId}&targetUserId={targetUserId}&firstMessageId={firstMessageId}&hintImageLink={hintImageLink}&hintVideoLink={hintVideoLink}")]
    string UpdateIWantUserByUserId(string userId, string targetUserId, string firstMessageId,
              string hintImageLink, string hintVideoLink, Stream data);
  }
}
