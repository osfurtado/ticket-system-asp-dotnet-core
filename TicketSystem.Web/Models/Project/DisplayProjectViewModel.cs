namespace TicketSystem.Web.Models.Project
{

    public class DisplayProjectViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Workflow { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

    }
}
