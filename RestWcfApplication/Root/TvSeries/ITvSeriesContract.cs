using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.TvSeries
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITvSeriesContract" in both code and config file together.
  [ServiceContract]
  public interface ITvSeriesContract
  {
    [OperationContract]
    [WebGet(UriTemplate = "search/{toSearch}")]
    List<TV_Series> SearchEpisodeByKeyphrase(string toSearch);

    [OperationContract]
    [WebGet(UriTemplate = "top?userId={userId}")]
    List<TV_Series> GetTopTvSeries(string userId);
  }
}
