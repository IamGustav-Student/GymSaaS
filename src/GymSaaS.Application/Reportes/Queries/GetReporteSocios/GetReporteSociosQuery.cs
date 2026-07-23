using GymSaaS.Application.Reportes.Queries.GetReporteIngresos;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Reportes.Queries.GetReporteSocios
{
    public class SociosMensualDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string MesLabel { get; set; } = string.Empty;
        public int Altas { get; set; }
        public int Bajas { get; set; }
    }

    public record GetReporteSociosQuery(int Meses = 6) : IRequest<List<SociosMensualDto>>;

    public class GetReporteSociosQueryHandler : IRequestHandler<GetReporteSociosQuery, List<SociosMensualDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetReporteSociosQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SociosMensualDto>> Handle(GetReporteSociosQuery request, CancellationToken cancellationToken)
        {
            var meses = Math.Clamp(request.Meses, 1, 24);
            var desde = DateTime.Today.AddMonths(-(meses - 1));
            desde = new DateTime(desde.Year, desde.Month, 1);

            // Altas: socios nuevos por mes de alta.
            var altas = await _context.Socios
                .AsNoTracking()
                .Where(s => s.FechaAlta >= desde)
                .GroupBy(s => new { s.FechaAlta.Year, s.FechaAlta.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count() })
                .ToListAsync(cancellationToken);

            // Bajas: membresías que terminaron su vigencia y ya no están activas
            // (venció naturalmente o se canceló) — se cuentan en el mes de fin.
            var bajas = await _context.MembresiasSocios
                .AsNoTracking()
                .Where(m => !m.Activa && m.FechaFin >= desde)
                .GroupBy(m => new { m.FechaFin.Year, m.FechaFin.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count() })
                .ToListAsync(cancellationToken);

            var resultado = new List<SociosMensualDto>();
            var cursor = DateTime.Today.AddMonths(-(meses - 1));

            for (var i = 0; i < meses; i++)
            {
                var anio = cursor.Year;
                var mes = cursor.Month;

                resultado.Add(new SociosMensualDto
                {
                    Anio = anio,
                    Mes = mes,
                    MesLabel = ReportesHelper.EtiquetaMes(anio, mes),
                    Altas = altas.FirstOrDefault(a => a.Year == anio && a.Month == mes)?.Cantidad ?? 0,
                    Bajas = bajas.FirstOrDefault(b => b.Year == anio && b.Month == mes)?.Cantidad ?? 0
                });

                cursor = cursor.AddMonths(1);
            }

            return resultado;
        }
    }
}
