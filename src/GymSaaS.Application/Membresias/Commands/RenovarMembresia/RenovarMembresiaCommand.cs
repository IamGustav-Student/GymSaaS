using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Application.Membresias.Commands.RenovarMembresia
{
    public class RenovarMembresiaCommand : IRequest<int>
    {
        public int SocioId { get; set; }
        public int TipoMembresiaId { get; set; }
    }

    public class RenovarMembresiaCommandHandler : IRequestHandler<RenovarMembresiaCommand, int>
    {
        private readonly IApplicationDbContext _context;

        public RenovarMembresiaCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> Handle(RenovarMembresiaCommand request, CancellationToken cancellationToken)
        {
            // 1. Obtener el Plan seleccionado
            var plan = await _context.TiposMembresia
                .FindAsync(new object[] { request.TipoMembresiaId }, cancellationToken);

            if (plan == null) throw new KeyNotFoundException("El plan seleccionado no existe.");

            // 2. Obtener la última membresía ACTIVA del socio para hacer el "Stacking" de fechas
            var ultimaMembresia = await _context.MembresiasSocios
                .Where(m => m.SocioId == request.SocioId && m.Activa)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefaultAsync(cancellationToken);

            // 3. Lógica de Fechas Inteligentes (Stacking)
            DateTime fechaInicio;

            if (ultimaMembresia != null && ultimaMembresia.FechaFin > DateTime.Now)
            {
                // Caso A: Socio previsor. Renueva antes de que venza.
                // La nueva arranca al día siguiente de que termine la actual.
                fechaInicio = ultimaMembresia.FechaFin.AddDays(1); // Ojo: Ajustar según tu lógica de hora (si fin es 23:59, +1s)
                // Para simplificar, usamos la misma fecha si el fin es fecha pura, o AddDays(0) si solapan.
                // Asumiremos que FechaFin es el último día válido. La nueva arranca al siguiente.
            }
            else
            {
                // Caso B: Socio nuevo o vencido. Arranca YA.
                fechaInicio = DateTime.Now;
            }

            var fechaFin = fechaInicio.AddDays(plan.DuracionDias);

            // 4. Crear la Membresía en estado "Pendiente de Pago" (Activa = false)
            // Cuando MercadoPago confirme el Webhook, la pasaremos a true.
            var nuevaMembresia = new MembresiaSocio
            {
                SocioId = request.SocioId,
                TipoMembresiaId = request.TipoMembresiaId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                PrecioPagado = plan.Precio,
                ClasesRestantes = plan.CantidadClases,
                Activa = false, // ¡Importante! No activar hasta pagar.
                
                // TenantId se llena automático por el contexto
            };

            _context.MembresiasSocios.Add(nuevaMembresia);
            await _context.SaveChangesAsync(cancellationToken);

            return nuevaMembresia.Id;
        }
    }
}