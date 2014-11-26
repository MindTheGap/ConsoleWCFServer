using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using RestWcfApplication.DB;
using System.ServiceModel.Activation;
using RestWcfApplication.Root.Token;
using RestWcfApplication.Root.Want;

namespace RestWcfApplication.Root.Episode
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class WantContract : IWantContract
  {
    public MessageState UpdateIWantUser(string userId, string token, string phoneNumber, 
      string hint, string hintImageLink, string hintVideoLink)
    {
      try
      {
        if (!TokenService.IsTokenValid(token, userId))
        {
          Debug.WriteLine("Got an invalid token from userId {0}", userId);
          return null;
        }

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userIdParsed = Convert.ToInt32(userId);

          var targetUser = context.Users.SingleOrDefault(u => u.PhoneNumber == phoneNumber);
          if (targetUser == null)
          {
            // TODO: user doesn't exist in the system yet so send him an SMS

            return new MessageState() { Type = @""};
          }

          var message = context.Messages.SingleOrDefault(m => m.== userIdParsed);

          

          return episode;
        }
      }
      catch (Exception e)
      {
        throw new FaultException("Something went wrong. exception is: " + e.Message);
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
