using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Reportes.Queries.GetReporteOcupacionClases
{
    public class OcupacionClaseDto
    {
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaHoraInicio { get; set; }
        public int CupoMaximo { get; set; }
        public int Reservados { get; set; }
        public double PorcentajeOcupacion { get; set; }
    }

    public class ReporteOcupacionDto
    {
        public double PromedioOcupacion { get; set; }
        public List<OcupacionClaseDto> Clases { get; set; } = new();
    }

    public record GetReporteOcupacionClasesQuery(int Dias = 30) : IRequest<ReporteOcupacionDto>;

    public class GetReporteOcupacionClasesQueryHandler : IRequestHandler<GetReporteOcupacionClasesQuery, ReporteOcupacionDto>
    {
        private readonly IApplicationDbContext _context;

        public GetReporteOcupacionClasesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReporteOcupacionDto> Handle(GetReporteOcupacionClasesQuery request, CancellationToken cancellationToken)
        {
            var dias = Math.Clamp(request.Dias, 1, 180);
            var desde = DateTime.Now.AddDays(-dias);
            var hasta = DateTime.Now.AddDays(dias);

            var clases = await _context.Clases
                .AsNoTracking()
                .Where(c => c.FechaHoraInicio >= desde && c.FechaHoraInicio <= hasta && c.CupoMaximo > 0)
                .Select(c => new
                {
                    c.Nombre,
                    c.FechaHoraInicio,
                    c.CupoMaximo,
                    Reservados = c.Reservas.Count(r => r.Activa)
                })
                .OrderByDescending(c => c.FechaHoraInicio)
                .ToListAsync(cancellationToken);

            var dtos = clases.Select(c => new OcupacionClaseDto
            {
                Nombre = c.Nombre,
                FechaHoraInicio = c.FechaHoraInicio,
                CupoMaximo = c.CupoMaximo,
                Reservados = c.Reservados,
                PorcentajeOcupacion = Math.Round(100.0 * c.Reservados / c.CupoMaximo, 1)
            }).ToList();

            return new ReporteOcupacionDto
            {
                PromedioOcupacion = dtos.Count > 0 ? Math.Round(dtos.Average(d => d.PorcentajeOcupacion), 1) : 0,
                Clases = dtos
            };
        }
    }
}
