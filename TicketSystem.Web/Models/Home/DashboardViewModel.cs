using TicketSystem.Web.Models.ProjectManagement;
using TicketSystem.Web.Models.Ticket;

namespace TicketSystem.Web.Models.Home
{
    public class DashboardViewModel
    {
        // Global Metrics (For App Admins)
        public int TotalAppUsers { get; set; }
        public int TotalActiveProjects { get; set; }
        public int TotalOpenTicketsGlobally { get; set; }

        // User-Specific Metrics (For current logged-in user)
        public int MyProjectsCount { get; set; }
        public int MyAssignedTicketsCount { get; set; }
        public int MyBlockedTicketsCount { get; set; }
        public int UnreadMessagesCount { get; set; }

        // Lists for Quick Access Tables
        public IEnumerable<TicketModel> RecentAssignedTickets { get; set; } = new List<TicketModel>();
        public IEnumerable<ProjectModel> MyRecentProjects { get; set; } = new List<ProjectModel>();

        // Flag to conditionally render UI components in Razor
        public bool IsAppAdmin { get; set; }
    }
}
