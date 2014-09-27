using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace RestWcfApplication.Root.Episode
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IEpisodeContract" in both code and config file together.
  [ServiceContract]
  public interface IEpisodeContract
  {
    [OperationContract]
    [WebGet(UriTemplate = "{id}")]
    DB.Episode GetEpisodeById(string id);

    [OperationContract]
    [WebGet(UriTemplate = "episodes/{tvSeriesId}")]
    List<DB.Episode> GetEpisodesByTvSeriesId(string tvSeriesId);

    [OperationContract]
    [WebGet(UriTemplate = "hel")]
    string Hello();
  }
}
