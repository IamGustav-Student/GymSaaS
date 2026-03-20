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
                .AsNoTracking()
                .Where(p => p.FechaPago.Month == DateTime.Now.Month && p.FechaPago.Year == DateTime.Now.Year && p.Pagado)
                .SumAsync(p => p.Monto, cancellationToken);

            var sociosActivos = await _context.Socios
                .AsNoTracking()
                .CountAsync(s => s.Activo, cancellationToken);

            var membresiasVendidasMes = await _context.Pagos
                .AsNoTracking()
                .Where(p => p.FechaPago.Month == DateTime.Now.Month && p.FechaPago.Year == DateTime.Now.Year && p.Pagado)
                .CountAsync(cancellationToken);

            // Calcular accesos de hoy
            var accesosHoy = await _context.Asistencias
                .AsNoTracking()
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