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
        public string MetodoPago { get; set; } = "Efectivo";
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

            if (tipoMembresia == null)
                throw new KeyNotFoundException("El plan de membresía seleccionado no existe.");

            var socio = await _context.Socios
                .FindAsync(new object[] { request.SocioId }, cancellationToken);

            if (socio == null)
                throw new KeyNotFoundException("El socio seleccionado no existe.");

            // Configuración Temporal
            DateTime fechaInicio = DateTime.Now;
            DateTime fechaFin = fechaInicio.AddDays(tipoMembresia.DuracionDias);

            // Lógica de activación inmediata solo para Efectivo
            bool activarAhora = (request.MetodoPago == "Efectivo");

            var entidad = new MembresiaSocio
            {
                SocioId = request.SocioId,
                TipoMembresiaId = request.TipoMembresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PrecioPagado = tipoMembresia.Precio,
                ClasesRestantes = tipoMembresia.CantidadClases,
                Activa = activarAhora,
                Estado = activarAhora ? "Activa" : "Pendiente de Pago"
            };

            _context.MembresiasSocios.Add(entidad);
            await _context.SaveChangesAsync(cancellationToken);

            // Registro automático en la caja si el pago fue en Efectivo
            if (activarAhora)
            {
                var pago = new Pago
                {
                    SocioId = request.SocioId,
                    MembresiaSocioId = entidad.Id,
                    FechaPago = DateTime.Now,
                    Monto = tipoMembresia.Precio,
                    MetodoPago = "Efectivo",
                    Pagado = true,
                    EstadoTransaccion = "approved"
                };
                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return entidad.Id;
        }
    }
}