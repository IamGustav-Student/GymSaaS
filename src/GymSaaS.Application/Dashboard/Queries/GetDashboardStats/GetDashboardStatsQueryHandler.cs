using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Dashboard.Queries.GetDashboardStats
{
    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _tenantService;

        public GetDashboardStatsQueryHandler(IApplicationDbContext context, ICurrentTenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            // 1. Obtener datos del Tenant actual
            var tenantId = _tenantService.TenantId;

            // Hack: Como Tenant no implementa IMustHaveTenant automático (es la raíz),
            // lo buscamos manualmente por el ID que tenemos en el servicio (que es el GUID).
            // Nota: En la fase de registro guardamos un Tenant, pero usamos un ID simulado.
            // Para mostrar el nombre correctamente, buscamos el Tenant cuyo ID coincida (si lo implementaste así)
            // O simplemente devolvemos "Mi Gimnasio" si no queremos hacer una query extra ahora.

            // Vamos a lo seguro: Estadísticas de negocio
            var totalSocios = await _context.Socios.CountAsync(cancellationToken);

            var sociosActivos = await _context.Socios
                .CountAsync(s => s.Activo, cancellationToken);

            // Membresías que vencieron y siguen marcadas como activas (o lógica de fecha)
            var hoy = DateTime.UtcNow.Date;
            var membresiasVencidas = await _context.MembresiasSocios
                .CountAsync(m => m.FechaFin < hoy && m.Activa, cancellationToken);

            // Sumar pagos del mes actual
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var ingresosMes = await _context.Pagos
                .Where(p => p.FechaPago >= inicioMes)
                .SumAsync(p => p.Monto, cancellationToken);

            return new DashboardStatsDto
            {
                NombreGimnasio = "Panel de Control", // Podríamos buscar el nombre del Tenant luego
                TotalSocios = totalSocios,
                SociosActivos = sociosActivos,
                MembresiasVencidas = membresiasVencidas,
                IngresosMes = ingresosMes
            };
        }
    }
}