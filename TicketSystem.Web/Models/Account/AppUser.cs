using Microsoft.AspNetCore.Identity;
using TicketSystem.Web.Models.Communication;
using TicketSystem.Web.Models.Ticket;

namespace TicketSystem.Web.Models.Account
{
    public class AppUser: IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<AppUserRole> UserRoles { get; set; } = [];
        public virtual ICollection<TicketModel> TicketsCreated { get; set; } = [];
        public virtual ICollection<TicketModel> TicketsAssignedToUser { get; set; } = [];
        public virtual ICollection<TicketModel> TicketsClosed { get; set; } = [];
        public virtual ICollection<TicketAttachment> FilesAttached { get; set; } = [];
        public virtual ICollection<TicketComment> Comments { get; set; } = [];
        public virtual ICollection<Message> MessagesSent { get; set; } = [];
        public virtual ICollection<Message> MessagesReceived { get; set; } = [];





    }
}
