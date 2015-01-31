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
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "getAllInitialMessages?userId={userId}")]
    string GetAllInitialMessages(string userId, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "getUserChatMessages?userId={userId}")]
    string GetUserChatMessages(string userId, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "readUserChatMessages?userId={userId}")]
    string ReadUserChatMessages(string userId, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "getUserContactsLastSeen?userId={userId}")]
    string GetUserContactsLastSeen(string userId, Stream stream);
  }
}
