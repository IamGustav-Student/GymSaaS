using System;
using System.Collections.Generic;

namespace GymSaaS.SharedUI.Models
{
    public class SocioDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Estado { get; set; } = "Activo";
        public string? UltimoAcceso { get; set; }
        
        public List<MembresiaDto> Membresias { get; set; } = new List<MembresiaDto>();
    }

    public class MembresiaDto
    {
        public int Id { get; set; }
        public string NombrePlan { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }
        public string Estado { get; set; } = string.Empty; // "Vigente", "Vencida", etc.
        public decimal PrecioPagado { get; set; }
    }

    public class DashboardStatsDto
    {
        public decimal IngresosMensuales { get; set; }
        public int SociosActivos { get; set; }
        public int NuevasMembresias { get; set; }
        public int AccesosHoy { get; set; }
    }

    public class TipoMembresiaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int DuracionDias { get; set; }
        public int? CantidadClases { get; set; }
        
        // Días de acceso
        public bool AccesoLunes { get; set; } = true;
        public bool AccesoMartes { get; set; } = true;
        public bool AccesoMiercoles { get; set; } = true;
        public bool AccesoJueves { get; set; } = true;
        public bool AccesoViernes { get; set; } = true;
        public bool AccesoSabado { get; set; } = true;
        public bool AccesoDomingo { get; set; } = true;
    }

    public class ClaseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public int DuracionMinutos { get; set; }
        public int CupoMaximo { get; set; }
        public int CupoReservado { get; set; }
        public int CupoActual { get; set; }
        public int CantidadEnEspera { get; set; }
        public bool Activa { get; set; }
        public decimal Precio { get; set; }
        public List<AsistenteDto> Asistentes { get; set; } = new();
    }

    public class AsistenteDto
    {
        public int ReservaId { get; set; }
        public string SocioNombre { get; set; } = string.Empty;
        public DateTime FechaReserva { get; set; }
    }
}
