using System.ComponentModel.DataAnnotations;
using TicketSystem.Web.Models.Project;

namespace TicketSystem.Web.Models.Ticket
{
    public class TicketDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public ProjectModel? Project { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string? AssigneeName { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<CommentViewModel> Comments { get; set; } = new();
        public List<AttachmentViewModel> Attachments { get; set; } = new();

        // Propriedades para os formulários na mesma página
        public AddCommentViewModel NewComment { get; set; } = new();
        public UploadAttachmentViewModel NewAttachment { get; set; } = new();
    }

    public class CommentViewModel
    {
        public string CreatorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AttachmentViewModel
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class AddCommentViewModel
    {
        public int TicketId { get; set; }
        [Required(ErrorMessage = "Comment cannot be empty")]
        public string Content { get; set; } = string.Empty;
    }

    public class UploadAttachmentViewModel
    {
        public int TicketId { get; set; }
        
        [Required(ErrorMessage = "Select please a file to Upload")]
        public IFormFile? File { get; set; }
    }
}
