using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Project.Proposta
{
    public class ProjectCreateViewModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        [DataType(DataType.Date)]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Selecione um Workflow.")]
        [Display(Name = "Workflow")]
        public int WorkflowId { get; set; }

        // Usado apenas para popular o dropdown na View
        public SelectList? WorkflowsList { get; set; }
    }
}
