using GymSaaS.Application.Clases.Queries.GetClases;
using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Queries.GetClaseById
{
    public record GetClaseByIdQuery(int Id) : IRequest<ClaseDto?>;

    public class GetClaseByIdQueryHandler : IRequestHandler<GetClaseByIdQuery, ClaseDto?>
    {
        private readonly IApplicationDbContext _context;

        public GetClaseByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ClaseDto?> Handle(GetClaseByIdQuery request, CancellationToken cancellationToken)
        {
            // 1. Traemos la entidad con sus relaciones
            var entity = await _context.Clases
                .AsNoTracking()
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Socio)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (entity == null) return null;

            // 2. Mapeamos manualmente para incluir la lista anidada
            return new ClaseDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Instructor = entity.Instructor,
                FechaHoraInicio = entity.FechaHoraInicio,
                DuracionMinutos = entity.DuracionMinutos,
                CupoMaximo = entity.CupoMaximo,
                CupoReservado = entity.CupoReservado,
                Activa = entity.Activa,
                Asistentes = entity.Reservas.Select(r => new AsistenteDto
                {
                    ReservaId = r.Id,
                    SocioNombre = $"{r.Socio.Nombre} {r.Socio.Apellido}",
                    FechaReserva = r.FechaReserva
                }).ToList()
            };
        }
    }
}