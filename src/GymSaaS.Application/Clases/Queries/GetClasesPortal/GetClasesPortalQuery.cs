using GymSaaS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Clases.Queries.GetClasesPortal
{
    public class ClasePortalDto
    {
        public int Id { get; set; }
        public string NombreClase { get; set; } = string.Empty;
        public string? Instructor { get; set; }
        public DateTime FechaHoraInicio { get; set; }
        public int DuracionMinutos { get; set; }
        public int CuposDisponibles { get; set; }
        public bool ReservadaPorUsuario { get; set; }

        // --- NUEVO CAMPO: Identificador único de la reserva del usuario ---
        public int? ReservaId { get; set; }
        // -----------------------------------------------------------------

        public decimal Precio { get; set; }
        public bool RequierePago => Precio > 0;
    }

    public record GetClasesPortalQuery(int SocioId) : IRequest<List<ClasePortalDto>>;

    public class GetClasesPortalQueryHandler : IRequestHandler<GetClasesPortalQuery, List<ClasePortalDto>>
    {
        private readonly IApplicationDbContext _context;

        public GetClasesPortalQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClasePortalDto>> Handle(GetClasesPortalQuery request, CancellationToken cancellationToken)
        {
            var hoy = DateTime.UtcNow.Date; // O TimeZone del Tenant

            // Traemos las clases futuras y activas
            var clases = await _context.Clases
                .Include(c => c.Reservas)
                .Where(c => c.FechaHoraInicio >= hoy && c.Activa)
                .OrderBy(c => c.FechaHoraInicio)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Mapeo eficiente
            var resultado = clases.Select(c => new ClasePortalDto
            {
                Id = c.Id,
                NombreClase = c.Nombre,
                Instructor = c.Instructor,
                FechaHoraInicio = c.FechaHoraInicio,
                DuracionMinutos = c.DuracionMinutos,
                Precio = c.Precio,

                // Cálculo de cupos reales
                CuposDisponibles = c.CupoMaximo - c.Reservas.Count(r => r.Activa),

                // Verificación: ¿Ya reservó?
                ReservadaPorUsuario = c.Reservas.Any(r => r.SocioId == request.SocioId && r.Activa),

                // --- INYECCIÓN DE DATO CRÍTICO ---
                // Obtenemos el ID exacto de la reserva de ESTE usuario para ESTA clase.
                // Usamos FirstOrDefault para obtener el ID o 0/null si no existe.
                ReservaId = c.Reservas
                    .Where(r => r.SocioId == request.SocioId && r.Activa)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefault()
                // ---------------------------------
            }).ToList();

            return resultado;
        }
    }
}