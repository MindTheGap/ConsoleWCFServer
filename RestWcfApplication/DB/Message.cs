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
    
    public partial class Message
    {
        public int Id { get; set; }
        public int SourceUserId { get; set; }
        public int TargetUserId { get; set; }
        public int FirstMessageId { get; set; }
        public string Date { get; set; }
        public int ReceivedState { get; set; }
        public Nullable<int> SystemMessageState { get; set; }
        public Nullable<int> HintId { get; set; }
    
        public virtual FirstMessage FirstMessage { get; set; }
        public virtual Hint Hint { get; set; }
        public virtual User SourceUser { get; set; }
        public virtual User TargetUser { get; set; }
    }
}
