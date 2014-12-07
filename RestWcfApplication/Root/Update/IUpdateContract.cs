using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RestWcfApplication.Root.Update
{
  [ServiceContract]
  public interface IUpdateContract
  {
    [OperationContract]
    [WebGet(UriTemplate = "getNewFirstUserMessages?userId={userId}&phoneNumber={phoneNumber}&startingFirstUserMessageId={startingFirstUserMessageId}")]
    string GetNewFirstUserMessages(string userId, string phoneNumber, string startingFirstUserMessageId);

    [OperationContract]
    [WebGet(UriTemplate = "updateFirstUserMessages?userId={userId}&phoneNumber={phoneNumber}&messagesIds={messagesIds}")]
    string UpdateFirstUserMessages(string userId, string phoneNumber, string messagesIds);

    [OperationContract]
    [WebGet(UriTemplate = "getUserChatMessages?sourceUserId={sourceUserId}&targetUserId={targetUserId}&startingMessageId={startingMessageId}")]
    string GetUserChatMessages(string sourceUserId, string targetUserId, string startingMessageId);

    [OperationContract]
    [WebGet(UriTemplate = "getUserContactsLastSeen?userId={userId}&phoneNumbersStr={phoneNumbersStr}")]
    string GetUserContactsLastSeen(string userId, string phoneNumbersStr);
  }
}
