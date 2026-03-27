using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Validation;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Project
{
    public class CreateProjectViewModel: IValidatableObject
    {
        [Required(ErrorMessage = "Title is Required!")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "Description is Required!")]
        public string Description { get; set; } = string.Empty;
        [Display(Name = "Start Date")]
        public DateOnly StartDate { get; set; }

        [Display(Name = "Workflow")]
        public int? WorkflowId { get; set; }

        public IEnumerable<SelectListItem>? Workflows { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validateContext)
        {
            // TODO: Move it to Edit Project
            /*
            if( EndDate <= StartDate)
            {
                yield return new ValidationResult(
                    "End date must be after Start date",
                    new[] { nameof(EndDate) }
                );
            }*/

            if (WorkflowId == null || WorkflowId <= 0)
            {
                yield return new ValidationResult(
                    "A valid Workflow must be indicated.",
                    new[] { nameof(WorkflowId) }
                );
            }


        }
    }
}
