﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RestWcfApplication.DB
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class Entities : DbContext
    {
        public Entities()
            : base("name=Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Token> Tokens { get; set; }
        public virtual DbSet<Hint> Hints { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<FirstMessage> FirstMessages { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
    
        public virtual int prEpisodeDelete(Nullable<int> episodeID)
        {
            var episodeIDParameter = episodeID.HasValue ?
                new ObjectParameter("EpisodeID", episodeID) :
                new ObjectParameter("EpisodeID", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("prEpisodeDelete", episodeIDParameter);
        }
    
        public virtual ObjectResult<prEpisodeInsert_Result> prEpisodeInsert(Nullable<int> tvSeriesID, Nullable<int> episodeNumber, string uRLPath, Nullable<int> seriesNumber)
        {
            var tvSeriesIDParameter = tvSeriesID.HasValue ?
                new ObjectParameter("TvSeriesID", tvSeriesID) :
                new ObjectParameter("TvSeriesID", typeof(int));
    
            var episodeNumberParameter = episodeNumber.HasValue ?
                new ObjectParameter("EpisodeNumber", episodeNumber) :
                new ObjectParameter("EpisodeNumber", typeof(int));
    
            var uRLPathParameter = uRLPath != null ?
                new ObjectParameter("URLPath", uRLPath) :
                new ObjectParameter("URLPath", typeof(string));
    
            var seriesNumberParameter = seriesNumber.HasValue ?
                new ObjectParameter("SeriesNumber", seriesNumber) :
                new ObjectParameter("SeriesNumber", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prEpisodeInsert_Result>("prEpisodeInsert", tvSeriesIDParameter, episodeNumberParameter, uRLPathParameter, seriesNumberParameter);
        }
    
        public virtual ObjectResult<prEpisodeSelect_Result> prEpisodeSelect(Nullable<int> episodeID)
        {
            var episodeIDParameter = episodeID.HasValue ?
                new ObjectParameter("EpisodeID", episodeID) :
                new ObjectParameter("EpisodeID", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prEpisodeSelect_Result>("prEpisodeSelect", episodeIDParameter);
        }
    
        public virtual ObjectResult<prEpisodeUpdate_Result> prEpisodeUpdate(Nullable<int> episodeID, Nullable<int> tvSeriesID, Nullable<int> episodeNumber, string uRLPath, Nullable<int> seriesNumber)
        {
            var episodeIDParameter = episodeID.HasValue ?
                new ObjectParameter("EpisodeID", episodeID) :
                new ObjectParameter("EpisodeID", typeof(int));
    
            var tvSeriesIDParameter = tvSeriesID.HasValue ?
                new ObjectParameter("TvSeriesID", tvSeriesID) :
                new ObjectParameter("TvSeriesID", typeof(int));
    
            var episodeNumberParameter = episodeNumber.HasValue ?
                new ObjectParameter("EpisodeNumber", episodeNumber) :
                new ObjectParameter("EpisodeNumber", typeof(int));
    
            var uRLPathParameter = uRLPath != null ?
                new ObjectParameter("URLPath", uRLPath) :
                new ObjectParameter("URLPath", typeof(string));
    
            var seriesNumberParameter = seriesNumber.HasValue ?
                new ObjectParameter("SeriesNumber", seriesNumber) :
                new ObjectParameter("SeriesNumber", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prEpisodeUpdate_Result>("prEpisodeUpdate", episodeIDParameter, tvSeriesIDParameter, episodeNumberParameter, uRLPathParameter, seriesNumberParameter);
        }
    
        public virtual int prTVSeriesDelete(Nullable<int> tvSeriesID)
        {
            var tvSeriesIDParameter = tvSeriesID.HasValue ?
                new ObjectParameter("TvSeriesID", tvSeriesID) :
                new ObjectParameter("TvSeriesID", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("prTVSeriesDelete", tvSeriesIDParameter);
        }
    
        public virtual ObjectResult<prTVSeriesInsert_Result> prTVSeriesInsert(string name, Nullable<int> hits, string seriesImagePath)
        {
            var nameParameter = name != null ?
                new ObjectParameter("Name", name) :
                new ObjectParameter("Name", typeof(string));
    
            var hitsParameter = hits.HasValue ?
                new ObjectParameter("Hits", hits) :
                new ObjectParameter("Hits", typeof(int));
    
            var seriesImagePathParameter = seriesImagePath != null ?
                new ObjectParameter("SeriesImagePath", seriesImagePath) :
                new ObjectParameter("SeriesImagePath", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prTVSeriesInsert_Result>("prTVSeriesInsert", nameParameter, hitsParameter, seriesImagePathParameter);
        }
    
        public virtual ObjectResult<prTVSeriesSelect_Result> prTVSeriesSelect(Nullable<int> tvSeriesID)
        {
            var tvSeriesIDParameter = tvSeriesID.HasValue ?
                new ObjectParameter("TvSeriesID", tvSeriesID) :
                new ObjectParameter("TvSeriesID", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prTVSeriesSelect_Result>("prTVSeriesSelect", tvSeriesIDParameter);
        }
    
        public virtual ObjectResult<prTVSeriesUpdate_Result> prTVSeriesUpdate(Nullable<int> tvSeriesID, string name, Nullable<int> hits, string seriesImagePath)
        {
            var tvSeriesIDParameter = tvSeriesID.HasValue ?
                new ObjectParameter("TvSeriesID", tvSeriesID) :
                new ObjectParameter("TvSeriesID", typeof(int));
    
            var nameParameter = name != null ?
                new ObjectParameter("Name", name) :
                new ObjectParameter("Name", typeof(string));
    
            var hitsParameter = hits.HasValue ?
                new ObjectParameter("Hits", hits) :
                new ObjectParameter("Hits", typeof(int));
    
            var seriesImagePathParameter = seriesImagePath != null ?
                new ObjectParameter("SeriesImagePath", seriesImagePath) :
                new ObjectParameter("SeriesImagePath", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prTVSeriesUpdate_Result>("prTVSeriesUpdate", tvSeriesIDParameter, nameParameter, hitsParameter, seriesImagePathParameter);
        }
    
        public virtual int prUserDelete(Nullable<int> id)
        {
            var idParameter = id.HasValue ?
                new ObjectParameter("Id", id) :
                new ObjectParameter("Id", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("prUserDelete", idParameter);
        }
    
        public virtual ObjectResult<prUserInsert_Result> prUserInsert(string email, Nullable<int> activity)
        {
            var emailParameter = email != null ?
                new ObjectParameter("Email", email) :
                new ObjectParameter("Email", typeof(string));
    
            var activityParameter = activity.HasValue ?
                new ObjectParameter("Activity", activity) :
                new ObjectParameter("Activity", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prUserInsert_Result>("prUserInsert", emailParameter, activityParameter);
        }
    
        public virtual ObjectResult<prUserSelect_Result> prUserSelect(Nullable<int> id)
        {
            var idParameter = id.HasValue ?
                new ObjectParameter("Id", id) :
                new ObjectParameter("Id", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prUserSelect_Result>("prUserSelect", idParameter);
        }
    
        public virtual ObjectResult<prUserUpdate_Result> prUserUpdate(Nullable<int> id, string email, Nullable<int> activity)
        {
            var idParameter = id.HasValue ?
                new ObjectParameter("Id", id) :
                new ObjectParameter("Id", typeof(int));
    
            var emailParameter = email != null ?
                new ObjectParameter("Email", email) :
                new ObjectParameter("Email", typeof(string));
    
            var activityParameter = activity.HasValue ?
                new ObjectParameter("Activity", activity) :
                new ObjectParameter("Activity", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<prUserUpdate_Result>("prUserUpdate", idParameter, emailParameter, activityParameter);
        }
    }
}
