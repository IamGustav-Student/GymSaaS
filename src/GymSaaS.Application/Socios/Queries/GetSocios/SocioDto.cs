namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    public class SocioDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Estado { get; set; } = "Activo"; // Activo / Inactivo
        public string? UltimoAcceso { get; set; }
    }
}