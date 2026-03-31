using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TicketSystem.Web.Models.Account;

namespace TicketSystem.Web.Models.Ticket
{
    public class DisplayTicketViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = string.Empty;
        [Display(Name = "Created At")]
        public DateOnly CreatedAt { get; set; }
        public string Assignee { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string CurrentStatus { get; set; } = string.Empty;

        public bool CanChange { get; set; }

    }
}
