using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Project.Proposta
{
    public class TicketCreateViewModel
    {
        [Required(ErrorMessage = "Título obrigatório.")]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public int ProjectId { get; set; } // Vem oculto (HiddenFor)

        // Opcional no momento da criação
        [Display(Name = "Atribuir a")]
        public string? AssigneeId { get; set; }
        public SelectList? UsersList { get; set; }
    }
}
