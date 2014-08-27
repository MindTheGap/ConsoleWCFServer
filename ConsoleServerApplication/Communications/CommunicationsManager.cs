using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ChildsTubeConsoleServer.Communications.Models;
using ChildsTubeConsoleServer.DB;
using ChildsTubeConsoleServer.Helpers;
using ChildsTubeConsoleServer.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace ChildsTubeConsoleServer.Communications
{
  public class CommunicationsManager
  {
    public MainWindowViewModel MainWindowViewModel { get; set; }

    public CommunicationsManager(MainWindowViewModel mainWindowViewModel)
    {
      MainWindowViewModel = mainWindowViewModel;
    }

    public void HandleSearchTvSeries(dynamic dynamicObject, HttpListenerContext context)
    {
      try
      {
        if (dynamicObject != null)
        {
          if (MainWindowViewModel != null)
          {
            JValue textObject = dynamicObject.Text;
            var strText = textObject.Value as string;

            if (strText == null)
            {
              MainWindowViewModel.LogManager.PrintWarningMessage("Text field (to search) is empty!");
              return;
            }

            dynamic sendFlexible = new ExpandoObject();

            var tvSeriesList = new List<TvSeries>();
            foreach (var tvSeries in MainWindowViewModel.MainDBEntities.TV_Series)
            {
              if (tvSeries.Name.ToLower().Contains(strText.ToLower()))
              {
                var newTvSeries = new TvSeries
                {
                  Name = tvSeries.Name,
                  SeriesImagePath = tvSeries.SeriesImagePath,
                  Hits = tvSeries.Hits
                };

                tvSeriesList.Add(newTvSeries);

                tvSeries.Hits++;
              }
            }

            sendFlexible.TvSeriesResults = tvSeriesList;

            SendMessage(context, sendFlexible);
          }
        }
      }
      catch (Exception exception)
      {
        if (MainWindowViewModel != null)
        {
          MainWindowViewModel.LogManager.PrintErrorMessage("Message has exception: " + exception.Message + ". InnerMessage: " + exception.InnerException);
        }
      }
    }

    public void HandleGetEpisodesForTvSeries(dynamic dynamicObject, HttpListenerContext context)
    {
      try
      {
        if (dynamicObject != null)
        {
          if (MainWindowViewModel != null)
          {
            var tvSeriesId = (int) dynamicObject.TvSeriesID.Value;
            dynamic sendFlexible = new ExpandoObject();

            var episodesList = GetEpisodesListForTvSeries(tvSeriesId);

            sendFlexible.Episodes = episodesList;

            SendMessage(context, sendFlexible);
          }
        }
      }
      catch (Exception exception)
      {
        if (MainWindowViewModel != null)
        {
          MainWindowViewModel.LogManager.PrintErrorMessage("Message has exception: " + exception.Message + ". InnerMessage: " + exception.InnerException);
        }
      }
    }

    private List<ExpandoObject> GetEpisodesListForTvSeries(int tvSeriesId)
    {
      var episodesList = new List<ExpandoObject>();
      var allEpisodesList =
        MainWindowViewModel.MainDBEntities.Episodes.Where(ep => ep.TvSeriesID == tvSeriesId).ToList();
      foreach (Episode ep in allEpisodesList)
      {
        dynamic newEpisode = new ExpandoObject();
        newEpisode.EpisodeNumber = ep.EpisodeNumber;
        newEpisode.SeriesNumber = ep.SeriesNumber;
        newEpisode.URLPath = ep.URLPath;

        episodesList.Add(newEpisode);
      }
      return episodesList;
    }

    public void HandleGetAllTvSeries(dynamic dynamicObject, HttpListenerContext context)
    {
      try
      {
        if (dynamicObject != null)
        {
          if (MainWindowViewModel != null)
          {
            dynamic sendFlexible = new ExpandoObject();

            var tvSeriesList = new List<ExpandoObject>();
            var originalList = MainWindowViewModel.MainDBEntities.TV_Series.ToList();
            originalList.Sort((x, y) => x.Hits - y.Hits);
            for (int i = originalList.Count - 1; i > originalList.Count - Definitions.NumberOfTvSeriesToSendAtStartup + 1; i--)
            {
              var tvSeries = originalList.ElementAt(i);

              dynamic newTvSeries = new ExpandoObject();
              newTvSeries.Name = tvSeries.Name;
              newTvSeries.SeriesImagePath = tvSeries.SeriesImagePath;
              newTvSeries.Hits = tvSeries.Hits;
              newTvSeries.Episodes = GetEpisodesListForTvSeries(tvSeries.TvSeriesID);

              tvSeriesList.Add(newTvSeries);
            }

            sendFlexible.TvSeries = tvSeriesList;

            SendMessage(context, sendFlexible);
          }
        }
      }
      catch (Exception exception)
      {
        if (MainWindowViewModel != null)
        {
          MainWindowViewModel.LogManager.PrintErrorMessage("Message has exception: " + exception.Message + ". InnerMessage: " + exception.InnerException);
        }
      }
    }

    #region Helpers

    //private bool UserExists(List<Member> usersList, string userEmailAddress, out Member userOut)
    //{
    //  if (userEmailAddress == null)
    //  {
    //    userOut = null;
    //    return false;
    //  }

    //  foreach (var user in usersList)
    //  {
    //    if (user.Email == userEmailAddress)
    //    {
    //      userOut = user;
    //      return true;
    //    }
    //  }

    //  userOut = null;

    //  return false;
    //}

    //private bool UserExists(List<Member> usersList, int internalUserId, out Member userOut)
    //{
    //  foreach (var user in usersList)
    //  {
    //    if (user.Member_ID == internalUserId)
    //    {
    //      userOut = user;
    //      return true;
    //    }
    //  }

    //  userOut = null;
    //  return false;
    //}

    private void SendMessage(HttpListenerContext context, dynamic dynamicObject)
    {
      // Obtain a response object.
      HttpListenerResponse response = context.Response;

      string responseString = JsonConvert.SerializeObject(dynamicObject);

      MainWindowViewModel.LogManager.PrintSuccessMessage("Server --> User: " + responseString);

      // Construct a response.
      byte[] buffer = Encoding.Unicode.GetBytes(responseString);
      // Get a response stream and write the response to it.
      response.ContentLength64 = buffer.Length;
      response.ContentEncoding = Encoding.Unicode;
      Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
    }

    #endregion Helpers

  }
}
