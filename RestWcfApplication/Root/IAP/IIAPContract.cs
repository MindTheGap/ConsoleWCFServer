using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RestWcfApplication.Root.IAP
{
  [ServiceContract]
  public interface IIAPContract
  {
    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "purchase?userId={userId}&token={token}")]
    string Purchase(string userId, string token, Stream stream);

    [OperationContract]
    [WebInvoke(Method = "POST",
      RequestFormat = WebMessageFormat.Json,
      BodyStyle = WebMessageBodyStyle.Bare,
      UriTemplate = "getPurchases?userId={userId}&token={token}")]
    string GetAllPurchases(string userId, string token, Stream stream);
  }
}
