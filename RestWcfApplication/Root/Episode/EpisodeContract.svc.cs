using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using RestWcfApplication.DB;
using System.ServiceModel.Activation;

namespace RestWcfApplication.Root.Episode
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "EpisodeContract" in code, svc and config file together.
  // NOTE: In order to launch WCF Test Client for testing this service, please select EpisodeContract.svc or EpisodeContract.svc.cs at the Solution Explorer and start debugging.
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class EpisodeContract : IEpisodeContract
  {
    public DB.Episode GetEpisodeById(string id)
    {
      try
      {
        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          int episodeId = Convert.ToInt32(id);
          var episode = context.Episodes.SingleOrDefault(e => e.EpisodeID == episodeId);

          return episode;
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    public List<DB.Episode> GetEpisodesByTvSeriesId(string tvSeriesId)
    {
      try
      {
        using (var context = new Entities())
        {
          return context.Episodes.Where(e => e.TvSeriesID == int.Parse(tvSeriesId)).ToList();
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }


    public void DeleteEpisode(string episodeIdToDelete, string userId)
    {
      // TODO: check if userId has permissions to delete episode

      try
      {
        using (var context = new Entities())
        {
          int episodeId = Convert.ToInt32(episodeIdToDelete);
          context.prEpisodeDelete(episodeId);
        }
      }
      catch
      {
        throw new FaultException("Something went wrong");
      }
    }

    public string Hello()
    {
      return "hello";
    }
  }
}
