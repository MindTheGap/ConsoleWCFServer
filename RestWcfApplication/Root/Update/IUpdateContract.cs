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
      UriTemplate = "getAllInitialMessages?userId={userId}&token={token}")]
    string GetAllInitialMessages(string userId, string token, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "readUserChatMessages?userId={userId}&token={token}")]
    string ReadUserChatMessages(string userId, string token, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST", UriTemplate = "getUserContactsLastSeenAndProfileImageLinks?userId={userId}&token={token}")]
    string GetUserContactsLastSeenAndProfileImageLinks(string userId, string token, Stream stream);
  }
}
