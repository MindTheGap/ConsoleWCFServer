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
    
    public partial class FirstMessage
    {
        public int Id { get; set; }
        public int SourceUserId { get; set; }
        public int TargetUserId { get; set; }
        public string Date { get; set; }
        public int LastMessageId { get; set; }
        public string SubjectName { get; set; }
    
        public virtual Message Message { get; set; }
        public virtual User SourceUser { get; set; }
        public virtual User TargetUser { get; set; }
    }
}