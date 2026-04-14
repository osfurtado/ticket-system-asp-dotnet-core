using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;

namespace TicketSystem.Web.Models.ProjectManagement
{
    public class ProjectDetailsViewModel
    {
        // --- HOME TAB ---
        public string CurrentUserId { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int TotalTickets { get; set; }
        public int TotalOpenedTickets { get; set; }
        public bool CanFinishProject { get; set; }
        public bool CanChangeProject { get; set; }

        // --- MEMBERS TAB ---
        public List<ProjectMemberItemViewModel> ExistingMembers { get; set; } = new();
        public AddProjectMemberViewModel AddMemberForm { get; set; } = new();

        public bool CanChangeMember { get; set; }

        // --- WORKFLOW TAB ---
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; } = string.Empty;
        public List<WorkflowStatusItemViewModel> WorkflowStatuses { get; set; } = new();
        public AddWorkflowStatusViewModel AddStatusForm { get; set; } = new();
    }

    // --- SUPPORTING VIEWMODELS ---

    public class ProjectMemberItemViewModel
    {
        public string MemberId { get; set; } = string.Empty;
        public string MemberUserId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string RoleInProject { get; set; } = string.Empty;

        public bool CanRemoveMember { get; set; }
        public string Initials { get; set; } = string.Empty;

        public List<MemberTicketsViewModel> TicketsAssigned = new();
        public bool IsOnline { get; set; }
    }

    public class MemberTicketsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class AddProjectMemberViewModel
    {
        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Please select a user.")]
        [Display(Name = "Select User")]
        public string SelectedUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role.")]
        [Display(Name = "Role")]
        public string SelectedRole { get; set; } = string.Empty;

        // Dropdowns to populate the UI Select elements
        public IEnumerable<SelectListItem>? AvailableUsers { get; set; }
        public IEnumerable<SelectListItem>? AvailableRoles { get; set; }
    }

    public class WorkflowStatusItemViewModel
    {
        public int StatusId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsInicial { get; set; }
        public bool IsFinal { get; set; }

        public int OrderIndex { get; set; }
    }

    public class AddWorkflowStatusViewModel
    {
        [Required]
        public int WorkflowId { get; set; }

        [Required]
        public int ProjectId { get; set; } // Needed to redirect back to Project Details after POST

        [Required(ErrorMessage = "Status name is required.")]
        public string Name { get; set; } = string.Empty;

        public bool IsInicial { get; set; }
        public bool IsFinal { get; set; }
    }

    public class StatusOrderDto
    {
        public int Id { get; set; }
        public int OrderIndex { get; set; }
    }
}
