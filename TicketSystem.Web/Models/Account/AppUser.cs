using Microsoft.AspNetCore.Identity;
using TicketSystem.Web.Models.Ticket;

namespace TicketSystem.Web.Models.Account
{
    public class AppUser: IdentityUser
    {
        public virtual ICollection<AppUserRole> UserRoles { get; set; } = [];
        public virtual ICollection<TicketModel> Tickets { get; set; } = [];
    }
}
