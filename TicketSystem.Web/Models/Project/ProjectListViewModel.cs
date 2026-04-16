namespace TicketSystem.Web.Models.Project
{
    public class ProjectListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DescriptionSnippet { get; set; } = string.Empty; 
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string WorkflowName { get; set; } = string.Empty;

        public bool IsCurrentUserMember { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; } 
    }
}
