using TicketSystem.Web.Models.Account;

namespace TicketSystem.Web.Models.Ticket
{
    public class TicketModel
    {
        public int Id { get; set; }
        public required string Title { get; set; }

        public required string Description { get; set; }
        public int ProjectId { get; set; }

        public required string CreatorId { get; set; }

        public DateTime CreatedAt { get; set; }

        public required string AssigneeId { get; set; }

        public DateTime? AssignedAt { get; set; }

        public string? ClosedById { get; set; }

        public DateTime? ClosedAt { get; set; }

        public int CurrentStatusId { get; set; }

        public AppUser? CreatedBy { get; set; }
    }
}
