namespace GymSaaS.Application.Dashboard.Queries.GetDashboardStats
{
    public class DashboardStatsDto
    {
        public int TotalSocios { get; set; }
        public int SociosActivos { get; set; }
        public int MembresiasVencidas { get; set; }
        public decimal IngresosMes { get; set; }
        public string NombreGimnasio { get; set; } = string.Empty;
    }
}