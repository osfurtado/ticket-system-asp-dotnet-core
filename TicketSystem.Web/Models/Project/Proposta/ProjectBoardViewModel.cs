using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketSystem.Web.Models.Project.Proposta
{
    public class ProjectBoardViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;

        public DateOnly? EndDate { get; set; }

        // Lista com os nomes dos status do Workflow (ex: "To Do", "In Progress", "Done")
        public List<string> WorkflowStatuses { get; set; } = [];
        public SelectList? UsersList { get; set; }
        // Tickets mapeados de forma simplificada para o quadro
        public List<TicketCardViewModel> Tickets { get; set; } = [];
    }

    public class TicketCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string? AssigneeName { get; set; } // Nome do responsável, se houver
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
        public bool IsBlocked { get; set; } // Calculado se BlockedByTickets.Any()
        public bool IsClosed { get; set; }
    }
}
