using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Ticket.ViewModels
{
    public class TicketEditViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Title")]
        [Required(ErrorMessage = "Title is required.")]
        public required string Title { get; set; }
        [Display(Name = "Description")]
        [Required(ErrorMessage = "Description is required.")]
        public required string Description { get; set; }
        [Display(Name = "Assignee")]
        public string? AssigneeId { get; set; }
        public DateTime? AssignedAt { get; set; }
        public IEnumerable<SelectListItem>? UsersList { get; set; }
        [Display(Name = "Status")]
        [Required(ErrorMessage = "Status is required.")]
        public required string CurrentStatus { get; set; }
        public IEnumerable<SelectListItem>? StatusList { get; set; }
        public int WorkflowId { get; set; }
        public int ProjectId { get; set; }
        public bool CanEdit { get; set; }
        public bool CanChangeStatus { get; set; }
        public bool CanManageDependencies { get; set; }
    }
}
