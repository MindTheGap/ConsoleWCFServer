using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text;
using RestWcfApplication.DB;

namespace RestWcfApplication.Root.TvSeries
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "TvSeriesContract" in code, svc and config file together.
  // NOTE: In order to launch WCF Test Client for testing this service, please select TvSeriesContract.svc or TvSeriesContract.svc.cs at the Solution Explorer and start debugging.
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class TvSeriesContract : ITvSeriesContract
  {
    private const int MaximumTvSeriesToReturn = 10;

    public List<TV_Series> SearchEpisodeByKeyphrase(string toSearch)
    {
      try
      {
        if (toSearch == "-1") toSearch = "";
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          return context.TV_Series.Where(tv => tv.Name.Contains(toSearch)).ToList();
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    public List<TV_Series> GetTopTvSeries(string userId)
    {
      // TODO: do something with the userId?
      

      try
      {
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var top = context.TV_Series.OrderBy(t => t.Hits);
          return top.Take(Math.Max(MaximumTvSeriesToReturn, top.Count())).ToList();
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message);
      }
    }
  }
}
