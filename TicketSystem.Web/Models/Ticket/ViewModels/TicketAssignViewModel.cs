using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Ticket.ViewModels
{
    public class TicketAssignViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Assign to")]
        [Required(ErrorMessage = "Please select a user to assign the ticket.")]
        public string AssigneeId { get; set; }
        public IEnumerable<SelectListItem>? UsersList { get; set; }

    }
}
