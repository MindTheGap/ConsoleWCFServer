using System.Collections.Generic;
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
    [WebGet(UriTemplate = "iamin?userId={userId}&sourcePhoneNumber={sourcePhoneNumber}&targetPhoneNumber={targetPhoneNumber}&hint={hint}&hintImageLink={hintImageLink}&hintVideoLink={hintVideoLink}")]
    string UpdateIWantUserByPhoneNumber(string userId, string sourcePhoneNumber, string targetPhoneNumber, 
      string hint, string hintImageLink, string hintVideoLink);
  }
}
