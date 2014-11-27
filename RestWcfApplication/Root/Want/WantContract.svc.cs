using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using RestWcfApplication.DB;
using System.ServiceModel.Activation;
using RestWcfApplication.Root.Token;
using RestWcfApplication.Root.Want;

namespace RestWcfApplication.Root.Want
{
  [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
  public class WantContract : IWantContract
  {
    public string UpdateIWantUser(string userId, string sourcePhoneNumber, string targetPhoneNumber,
      string hint, string hintImageLink, string hintVideoLink)
    {
      try
      {
        //if (!TokenService.IsTokenValid(token, userId))
        //{
        //  Debug.WriteLine("Got an invalid token from userId {0}", userId);
        //  return null;
        //}

        // TODO: check if userId corresponds to phoneNumber

        dynamic toSend = new ExpandoObject();

        using (var context = new Entities())
        {
          context.Configuration.ProxyCreationEnabled = false;

          var userIdParsed = Convert.ToInt32(userId);
          var sourceUser = context.Users.SingleOrDefault(u => u.Id == userIdParsed && u.PhoneNumber == sourcePhoneNumber);
          if (sourceUser == null)
          {
            toSend.Type = EMessageState.Error.ToString("d");
            toSend.ErrorInfo = ErrorInfo.SourceUserIdDoesNotExist.ToString("d");
            return toSend;
          }

          var targetUser = context.Users.SingleOrDefault(u => u.PhoneNumber == targetPhoneNumber);
          if (targetUser == null)
          {
            // target user doesn't exist in the system yet
            // TODO: Send him an SMS

            // TODO: check maybe we need to save 3 times to get identity ID field from DB context

            var newTargetUser = new DB.User() { PhoneNumber = targetPhoneNumber };
            var newHint = new DB.Hint()
            {
              Text = hint, 
              PictureLink = hintImageLink, 
              VideoLink = hintVideoLink
            };
            var newMessage = new DB.Message()
            {
              User = sourceUser,
              User1 = newTargetUser,
              Hint = newHint,
              MessageState = (int) EMessageState.SentSms
            };
            context.Hints.Add(newHint);
            context.Messages.Add(newMessage);
            context.Users.Add(newTargetUser);
            context.SaveChanges();

            toSend.Type = EMessageState.SentSms.ToString("d");
            return toSend;
          }

          // target user exists, so checking if target user is in source user also
          var message = context.Messages.SingleOrDefault(m => m.SourceUserId == targetUser.Id && m.TargetUserId == userIdParsed);
          if (message != null)
          {
            // target user is in source user also - love is in the air
            // enabling a chat between them by sending the target user id to the source user

            // TODO: enable a chat between them
            toSend.Type = EMessageState.BothSidesAreIn.ToString("d");
            toSend.TargetUserId = targetUser.Id;
            return toSend;
          }

          // target user does not want source user yet


          return toSend;
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
