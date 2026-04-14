using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketSystem.Web.Models.Project
{
    public class ProjectBoardViewModel
    {
        public int ProjectId { get; set; }
        public string ProjectTitle { get; set; } = string.Empty;
        public string WorkflowName { get; set; } = string.Empty;

        public bool CanCreateTicket { get; set; }
        public bool CanManage { get; set; }

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
        public string? AssigneeName { get; set; } 
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
        public bool IsBlocked { get; set; } 
        public bool IsLocked { get; set; }

        public bool CanAssign { get; set; }
        public bool IsClosed { get; set; }
    }
}
