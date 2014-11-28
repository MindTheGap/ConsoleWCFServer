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
    
    public partial class User
    {
        public User()
        {
            this.MessagesAsSourceUser = new HashSet<Message>();
            this.MessagesAsTargetUser = new HashSet<Message>();
            this.SystemMessagesAsSourceUser = new HashSet<SystemMessage>();
            this.SystemMessagesAsTargetUser = new HashSet<SystemMessage>();
        }
    
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string EmailPasswordHash { get; set; }
        public Nullable<int> FacebookUserId { get; set; }
        public Nullable<int> GoogleUserId { get; set; }
        public string PhoneNumber { get; set; }
    
        public virtual ICollection<Message> MessagesAsSourceUser { get; set; }
        public virtual ICollection<Message> MessagesAsTargetUser { get; set; }
        public virtual ICollection<SystemMessage> SystemMessagesAsSourceUser { get; set; }
        public virtual ICollection<SystemMessage> SystemMessagesAsTargetUser { get; set; }
    }
}
