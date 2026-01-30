using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Dashboard.Queries.GetDashboardStats
{
    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IApplicationDbContext _context;

        public GetDashboardStatsQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            // --- CORRECCIÓN FINAL ---
            // Antes (Probable error): Sumabas _context.MembresiasSocios.Sum(...)
            // Ahora (Correcto): Sumamos _context.Pagos.Sum(...)
            // La tabla Pagos solo tiene dinero real verificado (Efectivo o MP Aprobado).

            var totalIngresosMes = await _context.Pagos
                .Where(p => p.FechaPago.Month == DateTime.Now.Month && p.FechaPago.Year == DateTime.Now.Year)
                .SumAsync(p => p.Monto, cancellationToken);

            var sociosActivos = await _context.Socios
                .CountAsync(s => s.Activo, cancellationToken);

            var membresiasVendidasMes = await _context.Pagos // Usamos Pagos para contar ventas reales también
                .Where(p => p.FechaPago.Month == DateTime.Now.Month && p.FechaPago.Year == DateTime.Now.Year)
                .CountAsync(cancellationToken);

            // Calcular accesos de hoy
            var accesosHoy = await _context.Asistencias
                .Where(a => a.FechaHora.Date == DateTime.Now.Date)
                .CountAsync(cancellationToken);

            return new DashboardStatsDto
            {
                IngresosMensuales = totalIngresosMes,
                SociosActivos = sociosActivos,
                NuevasMembresias = membresiasVendidasMes,
                AccesosHoy = accesosHoy
            };
        }
    }
}