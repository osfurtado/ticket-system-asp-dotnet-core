using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Web.Models.Account
{
    public class RegisterViewModel
    {
        [Required] public string Name { get; set; }
        [Required] public string Username { get; set; }
        [Required, DataType(DataType.Password)] public string Password { get; set; }
    }
}
