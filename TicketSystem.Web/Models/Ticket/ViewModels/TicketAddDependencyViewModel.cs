using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Ticket.ViewModels
{
    public class TicketAddDependencyViewModel
    {
        [Required]
        public int BlockedTicketId { get; set; }
        [Required(ErrorMessage = "Select a Ticket please.")]
        public int BlockingTicketId { get; set; }
        public IEnumerable<SelectListItem> AvailableTickets { get; set; } = new List<SelectListItem>();
        public List<TicketModel> TicketsBlockingMe { get; set; } = new(); 
        public List<TicketModel> TicketsIBlock { get; set; } = new();

        public bool CanManageDependencies { get; set; }
    }
}
