using System.ComponentModel.DataAnnotations;

namespace GymSaaS.Web.Models
{
    public class RegisterTenantViewModel
    {
        [Required(ErrorMessage = "Nombre del Gimnasio requerido")]
        public string GymName { get; set; } = string.Empty;

        [Required]
        public string AdminName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }
}