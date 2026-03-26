using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Workflow
{

    public class WorkflowCreateViewModel
    {
        [Required(ErrorMessage = "Workflow name is Required")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        public List<WorkflowStatusViewModel> Statuses { get; set; } = new List<WorkflowStatusViewModel>();
    }



    public class WorkflowStatusViewModel
    {
        [Required(ErrorMessage = "Status name is required")]
        [Display(Name = "Status name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Initial?")]
        public bool IsInicial { get; set; }

        [Display(Name = "Final?")]
        public bool IsFinal { get; set; }
    }
}
