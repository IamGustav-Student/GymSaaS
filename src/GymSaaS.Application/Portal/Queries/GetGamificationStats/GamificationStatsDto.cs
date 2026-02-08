namespace GymSaaS.Application.Portal.Queries.GetGamificationStats
{
    public class GamificationStatsDto
    {
        public int RachaActual { get; set; } // Días consecutivos
        public int TotalAsistencias { get; set; }

        // Datos de Nivel
        public string NivelActual { get; set; } = "Novato";
        public string ColorNivel { get; set; } = "secondary"; // bootstrap color class
        public int AsistenciasParaSiguienteNivel { get; set; }
        public double PorcentajeProgreso { get; set; }

        // Datos del Socio
        public string NombreSocio { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
    }
}