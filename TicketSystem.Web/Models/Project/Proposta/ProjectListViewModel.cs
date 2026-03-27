namespace TicketSystem.Web.Models.Project.Proposta
{
    public class ProjectListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DescriptionSnippet { get; set; } = string.Empty; // Descrição truncada (ex: max 100 chars)
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string WorkflowName { get; set; } = string.Empty;

        // Dados calculados para a UI
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; } // Onde CurrentStatus != "Fechado"
    }
}
