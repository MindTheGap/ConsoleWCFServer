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
            this.SourceUserFirstMessages = new HashSet<FirstMessage>();
            this.TargetUserFirstMessages = new HashSet<FirstMessage>();
            this.SourceUserMessages = new HashSet<Message>();
            this.TargetUserMessages = new HashSet<Message>();
            this.Notifications = new HashSet<Notification>();
            this.SenderUserNotifications = new HashSet<Notification>();
        }
    
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string FacebookUserId { get; set; }
        public string GoogleUserId { get; set; }
        public string PhoneNumber { get; set; }
        public string ProfileImageLink { get; set; }
        public string LastSeen { get; set; }
        public string VerificationCode { get; set; }
        public bool Verified { get; set; }
        public string DeviceId { get; set; }
        public string DisplayName { get; set; }
        public int Coins { get; set; }
        public string Token { get; set; }
    
        public virtual ICollection<FirstMessage> SourceUserFirstMessages { get; set; }
        public virtual ICollection<FirstMessage> TargetUserFirstMessages { get; set; }
        public virtual ICollection<Message> SourceUserMessages { get; set; }
        public virtual ICollection<Message> TargetUserMessages { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Notification> SenderUserNotifications { get; set; }
    }
}
