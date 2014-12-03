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
    [WebGet(UriTemplate = "update?userId={userId}&phoneNumber={phoneNumber}")]
    string UpdateUserMessages(string userId, string phoneNumber);
  }
}
