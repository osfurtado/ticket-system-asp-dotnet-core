namespace TicketSystem.Web.Models.Ticket.ViewModels
{
    public class TicketFilterViewModel
    {
        // Filter Inputs
        public string? SearchTitle { get; set; }
        public DateTime? SearchDate { get; set; }
        public string? SearchCreatedBy { get; set; }
        public string? SearchProject { get; set; }
        public string? SearchAssignee { get; set; }
        public string? SearchStatus { get; set; }

        // Pagination Properties
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 8; // Set how many items you want per page
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }



        // Dropdown Data
        public List<string> ProjectList { get; set; } = new();
        public List<string> CreatedByList { get; set; } = new();
        public List<string> AssigneeList { get; set; } = new();

        // Results
        public List<TicketListViewModel> Tickets { get; set; } = new();
    }
}
