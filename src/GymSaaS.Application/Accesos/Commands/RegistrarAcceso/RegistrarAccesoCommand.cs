using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Accesos.Commands.RegistrarAcceso
{
    // Camino de acceso "asistido": molinetes/hardware de control de acceso o
    // recepción de gimnasios grandes. A diferencia de RegistrarIngresoQrCommand
    // (self check-in con QR dinámico + geolocalización, pensado para gimnasios
    // sin personal), acá no hay geofencing: el dispositivo/recepcionista ya está
    // físicamente en el gimnasio, así que la identidad se resuelve por DNI o
    // código de acceso (tarjeta/QR estático del socio).

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

    // El parámetro se llama 'Input' porque puede ser DNI o Código de Acceso (tarjeta/QR estático)
    public record RegistrarAccesoCommand(string Input) : IRequest<AccesoDto>;

    public class RegistrarAccesoCommandHandler : IRequestHandler<RegistrarAccesoCommand, AccesoDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentTenantService _currentTenantService;

        public RegistrarAccesoCommandHandler(IApplicationDbContext context, ICurrentTenantService currentTenantService)
        {
            _context = context;
            _currentTenantService = currentTenantService;
        }

        public async Task<AccesoDto> Handle(RegistrarAccesoCommand request, CancellationToken cancellationToken)
        {
            // 1. BUSQUEDA INTELIGENTE:
            // Buscamos si el input coincide con un DNI o con un Código de Acceso único
            // (el filtro global de tenant ya scopea esta consulta al gimnasio actual)
            var socio = await _context.Socios
                .FirstOrDefaultAsync(s => s.Dni == request.Input || s.CodigoAcceso == request.Input, cancellationToken);

            if (socio == null)
            {
                return new AccesoDto
                {
                    Permitido = false,
                    Mensaje = "Socio no encontrado / Código inválido",
                    Color = "danger"
                };
            }

            // 2. Buscamos sus membresías
            var membresias = await _context.MembresiasSocios
                .Include(m => m.TipoMembresia)
                .Where(m => m.SocioId == socio.Id && m.Activa)
                .OrderByDescending(m => m.FechaFin)
                .ToListAsync(cancellationToken);

            var ahora = DateTime.Now;

            // Deduplicación: evitamos registrar dos pasadas del mismo socio en menos de 2 minutos
            // (mismo criterio que usa el self check-in por QR)
            var ingresoReciente = await _context.Asistencias
                .Where(a => a.SocioId == socio.Id && a.Permitido && a.FechaHora > ahora.AddMinutes(-2))
                .OrderByDescending(a => a.FechaHora)
                .FirstOrDefaultAsync(cancellationToken);

            if (ingresoReciente != null)
            {
                return new AccesoDto
                {
                    Permitido = true,
                    Mensaje = "Acceso ya registrado (Pase libre).",
                    Color = "success",
                    SocioNombre = $"{socio.Nombre} {socio.Apellido}",
                    FotoUrl = socio.FotoUrl
                };
            }

            // Validamos fecha, clases restantes y día de acceso permitido por el plan
            var membresiaValida = membresias.FirstOrDefault(m =>
                m.FechaFin.Date >= ahora.Date &&
                (m.ClasesRestantes == null || m.ClasesRestantes > 0) &&
                m.TipoMembresia != null &&
                DiaPermitido(m.TipoMembresia, ahora.DayOfWeek)
            );

            // 3. Evaluamos resultado
            if (membresiaValida == null)
            {
                var vencida = membresias.FirstOrDefault();
                if (vencida != null && vencida.FechaFin.Date < ahora.Date)
                {
                    RegistrarAsistencia(socio, false, $"Membresía Vencida el {vencida.FechaFin:dd/MM/yyyy}");
                    await _context.SaveChangesAsync(cancellationToken);

                    return new AccesoDto
                    {
                        Permitido = false,
                        Mensaje = $"Membresía Vencida el {vencida.FechaFin:dd/MM/yyyy}",
                        Color = "danger",
                        SocioNombre = $"{socio.Nombre} {socio.Apellido}",
                        FotoUrl = socio.FotoUrl
                    };
                }

                RegistrarAsistencia(socio, false, "Sin Membresía Activa o plan no habilita el acceso hoy");
                await _context.SaveChangesAsync(cancellationToken);

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
            RegistrarAsistencia(socio, true, "Acceso Correcto");

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

        private void RegistrarAsistencia(Socio socio, bool permitido, string detalle)
        {
            _context.Asistencias.Add(new Asistencia
            {
                SocioId = socio.Id,
                FechaHora = DateTime.Now,
                Permitido = permitido,
                Detalle = detalle,
                TenantId = socio.TenantId,
                Tipo = "Molinete"
            });
        }

        private static bool DiaPermitido(TipoMembresia tipo, DayOfWeek dia) => dia switch
        {
            DayOfWeek.Monday => tipo.AccesoLunes,
            DayOfWeek.Tuesday => tipo.AccesoMartes,
            DayOfWeek.Wednesday => tipo.AccesoMiercoles,
            DayOfWeek.Thursday => tipo.AccesoJueves,
            DayOfWeek.Friday => tipo.AccesoViernes,
            DayOfWeek.Saturday => tipo.AccesoSabado,
            DayOfWeek.Sunday => tipo.AccesoDomingo,
            _ => false
        };
    }
}
