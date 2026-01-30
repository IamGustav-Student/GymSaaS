using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Commands.AsignarMembresia
{
    public class AsignarMembresiaCommand : IRequest<int>
    {
        public int SocioId { get; set; }
        public int TipoMembresiaId { get; set; }
        public string MetodoPago { get; set; } = "MercadoPago";
    }

    public class AsignarMembresiaCommandHandler : IRequestHandler<AsignarMembresiaCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public AsignarMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(AsignarMembresiaCommand request, CancellationToken cancellationToken)
        {
            var tipoMembresia = await _context.TiposMembresia
                .FindAsync(new object[] { request.TipoMembresiaId }, cancellationToken);

            if (tipoMembresia == null) throw new KeyNotFoundException("Plan no encontrado");

            // Configuración Inicial
            DateTime fechaInicio = DateTime.Now;
            DateTime fechaFin = fechaInicio.AddDays(tipoMembresia.DuracionDias);
            bool activarAhora = false;

            // --- LÓGICA CRÍTICA ---
            if (request.MetodoPago == "Efectivo")
            {
                // Solo si tengo el dinero en la mano activo YA
                activarAhora = true;

                // Stacking (Acumulación)
                var ultimaMembresia = await _context.MembresiasSocios
                    .Where(m => m.SocioId == request.SocioId && m.Activa)
                    .OrderByDescending(m => m.FechaFin)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ultimaMembresia != null && ultimaMembresia.FechaFin > DateTime.Now)
                {
                    fechaInicio = ultimaMembresia.FechaFin;
                    fechaFin = fechaInicio.AddDays(tipoMembresia.DuracionDias);
                }
            }
            else
            {
                // Si es MercadoPago, NO ACTIVAMOS.
                // Y lo más importante: NO REGISTRAMOS PAGO AÚN.
                activarAhora = false;
            }

            var entidad = new MembresiaSocio
            {
                SocioId = request.SocioId,
                TipoMembresiaId = request.TipoMembresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PrecioPagado = tipoMembresia.Precio,
                ClasesRestantes = tipoMembresia.CantidadClases,
                Activa = activarAhora // Será false para MP
            };

            _context.MembresiasSocios.Add(entidad);
            await _context.SaveChangesAsync(cancellationToken);

            // SOLO registramos en la caja (Tabla Pagos) si fue Efectivo
            if (activarAhora)
            {
                var pago = new Pago
                {
                    SocioId = request.SocioId,
                    MembresiaSocioId = entidad.Id,
                    FechaPago = DateTime.Now,
                    Monto = tipoMembresia.Precio,
                    MetodoPago = "Efectivo",
                    // TenantId se llena solo
                };
                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return entidad.Id;
        }
    }
}