using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.ProjectManagement
{
    public class PMCreateEditViewModel
    {
        public IEnumerable<ProjectListVM> Projects { get; set; } = [];
        public CreateProjectVM CreateForm { get; set; } = new();
        public EditProjectVM EditForm { get; set; } = new();
        public bool ShowCreateModal { get; set; } = false;
        public bool ShowEditModal { get; set; } = false;
    }

    public class ProjectListVM
    {
        public int Id { get; set; }
        public string Title { get; set; }
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }
        [Display(Name = "Start")]
        public DateOnly StartDate { get; set; }
        [Display(Name = "End")]
        public DateOnly? EndDate { get; set; }
        [Display(Name = "Members")]
        public int TotalMembers { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateProjectVM
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
    }

    public class EditProjectVM
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
    }
}
