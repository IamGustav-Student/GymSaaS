namespace GymSaaS.Application.Dashboard.Queries.GetDashboardStats
{
    public class DashboardStatsDto
    {
        // Coincide con: totalIngresosMes
        public decimal IngresosMensuales { get; set; }

        // Coincide con: sociosActivos
        public int SociosActivos { get; set; }

        // Coincide con: membresiasVendidasMes
        public int NuevasMembresias { get; set; }

        // Coincide con: accesosHoy
        public int AccesosHoy { get; set; }
    }
}