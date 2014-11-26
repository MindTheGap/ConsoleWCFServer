using System;
using System.Collections.Generic;
using System.Data.Entity;
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
    private const int MaximumTvSeriesToReturn = 3;

    public List<TV_Series> SearchTvSeriesByKeyphrase(string toSearch, string userId)
    {
      try
      {
        if (toSearch == "-1") toSearch = "";
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;
          var userIdInt = int.Parse(userId);

          var list = context.TV_Series.Include("Episodes").Where(tv => tv.Name.Contains(toSearch)).ToList();

          foreach (var tvSeries in list)
          {
            if (!context.Purchases.Any(p => p.TvSeriesId == tvSeries.TvSeriesID && p.UserId == userIdInt))
            {
              tvSeries.Episodes = tvSeries.Episodes.Take(1).ToList();
            }

            foreach (var episode in tvSeries.Episodes)
            {
              episode.TV_Series = null;
            }
          }

          return list;
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

          var userIdInt = int.Parse(userId);
          var userList = context.Users.Where(u => u.Id == userIdInt);
          var user = userList.Count() != 0 ? userList.First() : null;
          if (user != null)
          {
            var top = context.TV_Series.Include("Episodes").OrderBy(t => t.Hits).Take(MaximumTvSeriesToReturn).ToList();
            foreach (var tvSeries in top)
            {
              if (!context.Purchases.Any(p => p.TvSeriesId == tvSeries.TvSeriesID && p.UserId == userIdInt))
              {
                tvSeries.Episodes = tvSeries.Episodes.Take(1).ToList();
              }

              foreach (var episode in tvSeries.Episodes)
              {
                episode.TV_Series = null;
              }
            }

            return top;            
          }
        }

        return null;
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception: " + e.Message);
      }
    }
  }
}
