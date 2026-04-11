using System.ComponentModel.DataAnnotations.Schema;
using TicketSystem.Web.Models.Ticket;

namespace TicketSystem.Web.Models.Workflow
{
    public class WorkflowStatus
    {
        public int Id { get; set; }
        [ForeignKey("WorkflowModel")]
        public int WorkflowId { get; set; }

        public WorkflowModel? Workflow { get; set; }
        public required string Name { get; set; }
        public bool IsInicial { get; set; }
        public bool IsFinal { get; set; }

        public int OrderIndex { get; set; }

        public virtual ICollection<WorkflowStatusTransition> PreviousStatuses { get; set; } = [];
        public virtual ICollection<WorkflowStatusTransition> NextStatuses { get; set; } = [];

    }
}
