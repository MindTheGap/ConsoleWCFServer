using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace RestWcfApplication.Root.Register
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IRegisterContract" in both code and config file together.
  [ServiceContract]
  public interface IRegisterContract
  {
    /// <summary>
    /// registers the user to the system and returns his new userId
    /// </summary>
    /// <param name="userEmail"></param>
    /// <returns>userId</returns>
    [OperationContract]
    [WebGet(UriTemplate = "{userEmail}")]
    int Register(string userEmail);
  }
}
