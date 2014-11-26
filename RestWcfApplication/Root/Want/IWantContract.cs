using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace RestWcfApplication.Root.Want
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IEpisodeContract" in both code and config file together.
  [ServiceContract]
  public interface IWantContract
  {
    [OperationContract]
    [WebGet(UriTemplate = "want?userId={userId}&token={token}&phone={phoneNumber}&hint={hint}&hintImageLink={hintImageLink}&hintVideoLink={hintVideoLink}")]
    DB.MessageState UpdateIWantUser(string userId, string token, string phoneNumber, string hint, string hintImageLink, string hintVideoLink);

    [OperationContract]
    [WebGet(UriTemplate = "episodes/{tvSeriesId}")]
    List<DB.Episode> GetEpisodesByTvSeriesId(string tvSeriesId);

    [OperationContract]
    [WebGet(UriTemplate = "hel")]
    string Hello();
  }
}
