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
    
    public partial class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; }
        public Nullable<int> CoinAmount { get; set; }
        public Nullable<int> SenderUserId { get; set; }
    
        public virtual User User { get; set; }
        public virtual User SenderUser { get; set; }
    }
}
