using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TicketSystem.Web.Models.Account;
using TicketSystem.Web.Models.Ticket;
using TicketSystem.Web.Models.Workflow;

namespace TicketSystem.Web.Models.ProjectManagement
{
    public class ProjectModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }

        public required string Description { get; set; }
        [Display(Name = "Created By")]
        public string CreatedById { get; set; }

        public AppUser? CreatedBy { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [ForeignKey("WorkflowModel")]
        public int WorkflowId { get; set; }

        public WorkflowModel? Workflow { get; set; }

        public bool IsDeleted { get; set; }

        public Guid? InviteToken { get; set; }

        public virtual ICollection<TicketModel> Tickets { get; set; } = [];

        public virtual ICollection<ProjectMember> Members { get; set; } = [];
    }
}
