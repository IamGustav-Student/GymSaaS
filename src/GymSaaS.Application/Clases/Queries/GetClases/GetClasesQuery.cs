using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Queries.GetClases
{
    public record GetClasesQuery : IRequest<List<ClaseDto>>;

    public class GetClasesQueryHandler : IRequestHandler<GetClasesQuery, List<ClaseDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClasesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClaseDto>> Handle(GetClasesQuery request, CancellationToken cancellationToken)
        {
            // FILTRO OPERATIVO:
            // 1. Solo clases ACTIVAS (Soft Delete oculto)
            // 2. Solo clases FUTURAS (Histórico oculto para gestión diaria)
            // Nota: No usamos Global Query Filter para la fecha porque queremos conservar la data para reportes.

            var fechaCorte = DateTime.Now; // Usamos hora del servidor

            var clases = await _context.Clases
                .AsNoTracking()
                .Include(c => c.Reservas)
                .Include(c => c.ListaEspera)
                .Where(c => c.Activa == true) // Condición de Actividad
                .Where(c => c.FechaHoraInicio >= fechaCorte) // Condición de Tiempo
                .OrderBy(c => c.FechaHoraInicio) // Orden ascendente (lo más próximo arriba)
                .Select(c => new ClaseDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Instructor = c.Instructor,
                    FechaHoraInicio = c.FechaHoraInicio,
                    CupoMaximo = c.CupoMaximo,
                    CupoActual = c.Reservas.Count(r => r.Activa), // Contamos solo reservas vivas
                    CantidadEnEspera = c.ListaEspera.Count
                })
                .ToListAsync(cancellationToken);

            return clases;
        }
    }
}