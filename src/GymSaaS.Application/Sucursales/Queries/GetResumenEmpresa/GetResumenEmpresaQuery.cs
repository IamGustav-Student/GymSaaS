using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Sucursales.Queries.GetResumenEmpresa
{
    public class ResumenEmpresaDto
    {
        public int CantidadSucursales { get; set; }
        public int SociosActivosTotal { get; set; }
        public decimal IngresosMensualesTotal { get; set; }
    }

    // Resumen consolidado básico: suma socios activos e ingresos del mes de
    // TODAS las sucursales de la Empresa actual (cada una sigue facturando y
    // administrando sus socios por separado, esto es solo una vista agregada).
    public record GetResumenEmpresaQuery : IRequest<ResumenEmpresaDto>;

    public class GetResumenEmpresaQueryHandler : IRequestHandler<GetResumenEmpresaQuery, ResumenEmpresaDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public GetResumenEmpresaQueryHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task<ResumenEmpresaDto> Handle(GetResumenEmpresaQuery request, CancellationToken cancellationToken)
        {
            var tenantIdStr = _currentTenantService.TenantId;
            if (string.IsNullOrEmpty(tenantIdStr) || !int.TryParse(tenantIdStr, out var tenantId))
                return new ResumenEmpresaDto();

            var tenantActual = await _context.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenantActual == null) return new ResumenEmpresaDto();

            var tenantIds = tenantActual.EmpresaId.HasValue
                ? await _context.Tenants
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(t => t.EmpresaId == tenantActual.EmpresaId.Value)
                    .Select(t => t.Id.ToString())
                    .ToListAsync(cancellationToken)
                : new List<string> { tenantActual.Id.ToString() };

            var sociosActivos = await _context.Socios
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(s => tenantIds.Contains(s.TenantId) && s.Activo && !s.IsDeleted, cancellationToken);

            var ingresosMes = await _context.Pagos
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => tenantIds.Contains(p.TenantId)
                    && p.Pagado
                    && p.FechaPago.Month == DateTime.Now.Month
                    && p.FechaPago.Year == DateTime.Now.Year)
                .SumAsync(p => p.Monto, cancellationToken);

            return new ResumenEmpresaDto
            {
                CantidadSucursales = tenantIds.Count,
                SociosActivosTotal = sociosActivos,
                IngresosMensualesTotal = ingresosMes
            };
        }
    }
}
