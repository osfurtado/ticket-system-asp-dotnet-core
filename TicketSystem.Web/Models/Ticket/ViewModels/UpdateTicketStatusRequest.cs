namespace TicketSystem.Web.Models.Ticket.ViewModels
{
    public class UpdateTicketStatusRequest
    {
        public int TicketId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
