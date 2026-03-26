using System.ComponentModel.DataAnnotations.Schema;
using TicketSystem.Web.Models.Ticket;
using TicketSystem.Web.Models.Workflow;

namespace TicketSystem.Web.Models.Project
{
    public class ProjectModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }

        public required string Description { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        [ForeignKey("WorkflowModel")]
        public int WorkflowId { get; set; }

        public WorkflowModel? Workflow { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<TicketModel> Tickets { get; set; } = [];



    }
}
