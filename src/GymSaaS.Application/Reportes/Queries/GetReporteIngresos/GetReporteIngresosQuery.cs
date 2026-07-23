using System.Globalization;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Reportes.Queries.GetReporteIngresos
{
    public class IngresoMensualDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string MesLabel { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public record GetReporteIngresosQuery(int Meses = 6) : IRequest<List<IngresoMensualDto>>;

    public class GetReporteIngresosQueryHandler : IRequestHandler<GetReporteIngresosQuery, List<IngresoMensualDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetReporteIngresosQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IngresoMensualDto>> Handle(GetReporteIngresosQuery request, CancellationToken cancellationToken)
        {
            var meses = Math.Clamp(request.Meses, 1, 24);
            var desde = DateTime.Today.AddMonths(-(meses - 1)).Date;
            desde = new DateTime(desde.Year, desde.Month, 1);

            var pagos = await _context.Pagos
                .AsNoTracking()
                .Where(p => p.Pagado && p.FechaPago >= desde)
                .GroupBy(p => new { p.FechaPago.Year, p.FechaPago.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Monto) })
                .ToListAsync(cancellationToken);

            return ReportesHelper.RellenarMeses(meses, (anio, mes) =>
            {
                var encontrado = pagos.FirstOrDefault(p => p.Year == anio && p.Month == mes);
                return new IngresoMensualDto
                {
                    Anio = anio,
                    Mes = mes,
                    MesLabel = ReportesHelper.EtiquetaMes(anio, mes),
                    Total = encontrado?.Total ?? 0
                };
            });
        }
    }

    internal static class ReportesHelper
    {
        public static List<T> RellenarMeses<T>(int meses, Func<int, int, T> factory)
        {
            var resultado = new List<T>();
            var cursor = DateTime.Today.AddMonths(-(meses - 1));

            for (var i = 0; i < meses; i++)
            {
                resultado.Add(factory(cursor.Year, cursor.Month));
                cursor = cursor.AddMonths(1);
            }

            return resultado;
        }

        public static string EtiquetaMes(int anio, int mes)
        {
            var nombre = new DateTime(anio, mes, 1).ToString("MMM", CultureInfo.GetCultureInfo("es-AR"));
            return $"{nombre} {anio}";
        }
    }
}
