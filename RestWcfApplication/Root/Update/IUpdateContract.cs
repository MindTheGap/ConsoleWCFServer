using System;
using System.Collections.Generic;
using System.IO;
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
    [WebInvoke(Method = "POST", UriTemplate = "updateFirstUserMessages?userId={userId}&phoneNumber={phoneNumber}")]
    string UpdateFirstUserMessages(string userId, string phoneNumber, Stream stream);

    [OperationContract]
    [WebGet(UriTemplate = "getUserChatMessages?sourceUserId={sourceUserId}&targetUserId={targetUserId}&startingMessageId={startingMessageId}")]
    string GetUserChatMessages(string sourceUserId, string targetUserId, string startingMessageId);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "getUserContactsLastSeen?userId={userId}")]
    string GetUserContactsLastSeen(string userId, Stream stream);
  }
}
