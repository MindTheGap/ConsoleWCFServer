//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class TV_Series
    {
        public TV_Series()
        {
            this.Episodes = new HashSet<Episode>();
        }
    
        public int TvSeriesID { get; set; }
        public string Name { get; set; }
        public int Hits { get; set; }
        public string SeriesImagePath { get; set; }
    
        public virtual ICollection<Episode> Episodes { get; set; }
    }
}
