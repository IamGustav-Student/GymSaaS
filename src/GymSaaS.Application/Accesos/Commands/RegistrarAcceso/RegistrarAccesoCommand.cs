using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Accesos.Commands.RegistrarAcceso
{
    // Retornamos un objeto con el resultado visual
    public record AccesoResultDto(bool Permitido, string Mensaje, string SocioNombre, string? FotoUrl);

    public record RegistrarAccesoCommand : IRequest<AccesoResultDto>
    {
        public string Dni { get; init; } = string.Empty;
    }

    public class RegistrarAccesoCommandHandler : IRequestHandler<RegistrarAccesoCommand, AccesoResultDto>
    {
        private readonly IApplicationDbContext _context;

        public RegistrarAccesoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccesoResultDto> Handle(RegistrarAccesoCommand request, CancellationToken cancellationToken)
        {
            // 1. Buscar socio por DNI
            var socio = await _context.Socios
                .Include(s => s.Membresias) // Traer historial
                .FirstOrDefaultAsync(s => s.Dni == request.Dni, cancellationToken);

            if (socio == null)
            {
                return new AccesoResultDto(false, "SOCIO NO ENCONTRADO", "Desconocido", null);
            }

            // 2. Validar Membresía Activa
            var hoy = DateTime.UtcNow.Date;

            var membresiaActiva = socio.Membresias
                .Where(m => m.Activa && m.FechaInicio <= hoy && m.FechaFin >= hoy)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefault();

            bool permitido = false;
            string mensaje = "";

            if (membresiaActiva != null)
            {
                // Verificar Cupos (si aplica)
                if (membresiaActiva.ClasesRestantes.HasValue)
                {
                    if (membresiaActiva.ClasesRestantes.Value > 0)
                    {
                        membresiaActiva.ClasesRestantes -= 1; // Descontar clase
                        permitido = true;
                        mensaje = $"BIENVENIDO (Quedan {membresiaActiva.ClasesRestantes} clases)";
                    }
                    else
                    {
                        permitido = false;
                        mensaje = "SIN CLASES DISPONIBLES";
                    }
                }
                else
                {
                    // Pase Libre
                    permitido = true;
                    mensaje = "BIENVENIDO (Pase Libre)";
                }
            }
            else
            {
                permitido = false;
                mensaje = "MEMBRESÍA VENCIDA O INEXISTENTE";
            }

            // 3. Registrar el Evento (Log)
            var asistencia = new Asistencia
            {
                SocioId = socio.Id,
                FechaHora = DateTime.UtcNow,
                Permitido = permitido,
                Detalle = mensaje
            };

            _context.Asistencias.Add(asistencia);
            await _context.SaveChangesAsync(cancellationToken);

            return new AccesoResultDto(permitido, mensaje, $"{socio.Nombre} {socio.Apellido}", socio.FotoUrl);
        }
    }
}