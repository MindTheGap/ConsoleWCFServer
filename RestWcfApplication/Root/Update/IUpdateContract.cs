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
    [WebGet(UriTemplate = "getUserMessages?userId={userId}&phoneNumber={phoneNumber}")]
    string GetUserMessages(string userId, string phoneNumber);

    [OperationContract]
    [WebGet(UriTemplate = "getUserChatMessages?sourceUserId={sourceUserId}&targetUserId={targetUserId}&startingMessageId={startingMessageId}")]
    string GetUserChatMessages(string sourceUserId, string targetUserId, string startingMessageId);
  }
}
