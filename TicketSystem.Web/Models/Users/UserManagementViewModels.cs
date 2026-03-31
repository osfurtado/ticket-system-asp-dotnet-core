using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Users
{
    public class UserListViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string RoleId { get; set; }
        [Display(Name = "Status Active")] public bool IsActive { get; set; }
    }

    // Para o Admin editar usuários e roles
    public class EditUserViewModel
    {
        public string Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public string Username { get; set; }
        [Display(Name = "Role")] public string RoleId { get; set; }
        [Display(Name = "Status Active")] public bool IsActive { get; set; }
    }

    // Para a área pública
    public class RegisterViewModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Username { get; set; }
        [Required, DataType(DataType.Password)] public string Password { get; set; }
    }

    // Para o usuário gerir o próprio perfil
    public class ProfileViewModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Username { get; set; }
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; } // Opcional, só preenche se quiser mudar
    }

}
