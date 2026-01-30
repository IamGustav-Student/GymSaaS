namespace GymSaaS.Application.Socios.Queries.GetSocios
{
    public class SocioDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;   // Nuevo
        public string Apellido { get; set; } = string.Empty; // Nuevo
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; } // Nuevo
        public string Estado { get; set; } = "Activo";
        public string? UltimoAcceso { get; set; }

        // Lista de Membresías para el Timeline
        public List<MembresiaDto> Membresias { get; set; } = new List<MembresiaDto>();
    }

    // Sub-DTO para mostrar las membresías en el perfil
    public class MembresiaDto
    {
        public int Id { get; set; }
        public string NombrePlan { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }
        public string Estado { get; set; } = string.Empty; // "En Curso", "Futura", "Vencida"
        public decimal PrecioPagado { get; set; }
    }
}