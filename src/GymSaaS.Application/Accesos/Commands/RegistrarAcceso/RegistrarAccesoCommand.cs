using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Accesos.Commands.RegistrarAcceso
{
    // DTO de respuesta (Semáforo)
    public class AccesoDto
    {
        public bool Permitido { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Color { get; set; } = "danger"; // danger, success, warning
        public string? SocioNombre { get; set; }
        public string? FotoUrl { get; set; }
        public int? ClasesRestantes { get; set; }
    }

    // El parámetro se llama 'Input' porque puede ser DNI o Código QR (GUID)
    public record RegistrarAccesoCommand(string Input) : IRequest<AccesoDto>;

    public class RegistrarAccesoCommandHandler : IRequestHandler<RegistrarAccesoCommand, AccesoDto>
    {
        private readonly IApplicationDbContext _context;

        public RegistrarAccesoCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccesoDto> Handle(RegistrarAccesoCommand request, CancellationToken cancellationToken)
        {
            // 1. BUSQUEDA INTELIGENTE:
            // Buscamos si el input coincide con un DNI o con un Código QR único
            var socio = await _context.Socios
                .FirstOrDefaultAsync(s => s.Dni == request.Input || s.CodigoAcceso == request.Input, cancellationToken);

            if (socio == null)
            {
                return new AccesoDto
                {
                    Permitido = false,
                    Mensaje = "Socio no encontrado / QR Inválido",
                    Color = "danger"
                };
            }

            // 2. Buscamos sus membresías
            var membresias = await _context.MembresiasSocios
                .Include(m => m.TipoMembresia)
                .Where(m => m.SocioId == socio.Id && m.Activa)
                .OrderByDescending(m => m.FechaFin)
                .ToListAsync(cancellationToken);

            // Validamos fecha y clases (ignorando hora)
            var membresiaValida = membresias.FirstOrDefault(m =>
                m.FechaFin.Date >= DateTime.Now.Date &&
                (m.ClasesRestantes == null || m.ClasesRestantes > 0)
            );

            // 3. Evaluamos resultado
            if (membresiaValida == null)
            {
                var vencida = membresias.FirstOrDefault();
                if (vencida != null && vencida.FechaFin.Date < DateTime.Now.Date)
                {
                    return new AccesoDto
                    {
                        Permitido = false,
                        Mensaje = $"Membresía Vencida el {vencida.FechaFin:dd/MM/yyyy}",
                        Color = "danger",
                        SocioNombre = $"{socio.Nombre} {socio.Apellido}",
                        FotoUrl = socio.FotoUrl
                    };
                }

                return new AccesoDto
                {
                    Permitido = false,
                    Mensaje = "Sin Membresía Activa",
                    Color = "warning",
                    SocioNombre = $"{socio.Nombre} {socio.Apellido}",
                    FotoUrl = socio.FotoUrl
                };
            }

            // 4. Registramos el acceso (Permitido)
            var acceso = new Asistencia
            {
                SocioId = socio.Id,
                FechaHora = DateTime.Now,
                Permitido = true,
                Detalle = "Ingreso QR/DNI"
            };

            _context.Asistencias.Add(acceso);

            // 5. Consumimos clase si corresponde
            if (membresiaValida.ClasesRestantes.HasValue)
            {
                membresiaValida.ClasesRestantes -= 1;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new AccesoDto
            {
                Permitido = true,
                Mensaje = "¡Bienvenido!",
                Color = "success",
                SocioNombre = $"{socio.Nombre} {socio.Apellido}",
                FotoUrl = socio.FotoUrl,
                ClasesRestantes = membresiaValida.ClasesRestantes
            };
        }
    }
}