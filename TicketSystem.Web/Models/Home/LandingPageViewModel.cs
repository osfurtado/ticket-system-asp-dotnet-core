namespace TicketSystem.Web.Models.Home
{
    public class LandingPageViewModel
    {
        // Tickets
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ClosedTickets { get; set; }

        // Projects
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int ClosedProjects { get; set; }

        // Users
        public int TotalUsers { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new Dictionary<string, int>();
    }
}

