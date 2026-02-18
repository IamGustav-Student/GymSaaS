namespace GymSaaS.Web.Models
{
    public class OpcionGimnasioViewModel
    {
        public int SocioId { get; set; }
        public string NombreGimnasio { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string? TenantId { get; internal set; }
    }
}