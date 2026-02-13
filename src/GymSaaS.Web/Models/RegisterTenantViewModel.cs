using System.ComponentModel.DataAnnotations;

namespace GymSaaS.Web.Models
{
    public class RegisterTenantViewModel
    {
        [Required(ErrorMessage = "El nombre del gimnasio es obligatorio")]
        public string GymName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tu nombre es obligatorio")]
        public string AdminName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debes seleccionar un plan de suscripción para continuar")]
        public string SelectedPlan { get; set; } = string.Empty;

        // Propiedad opcional para mostrar errores en la vista
        public string? ErrorMessage { get; set; }
    }
}