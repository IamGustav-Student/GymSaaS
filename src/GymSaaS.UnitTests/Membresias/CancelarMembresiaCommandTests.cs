using FluentAssertions;
using GymSaaS.Application.Membresias.Commands.CancelarMembresia;
using GymSaaS.Domain.Entities;
using GymSaaS.UnitTests.TestHelpers;
using Xunit;

namespace GymSaaS.UnitTests.Membresias
{
    public class CancelarMembresiaCommandTests
    {
        [Fact]
        public async Task Handle_MembresiaActiva_DeberiaDesactivarYMarcarComoCancelada()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var membresia = new MembresiaSocio
            {
                SocioId = 1,
                TipoMembresiaId = 1,
                FechaInicio = DateTime.UtcNow.AddDays(-5),
                FechaFin = DateTime.UtcNow.AddDays(25),
                Activa = true,
                Estado = "Activa",
                TenantId = "tenant-1"
            };
            context.MembresiasSocios.Add(membresia);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new CancelarMembresiaCommandHandler(context);
            await handler.Handle(new CancelarMembresiaCommand(membresia.Id), CancellationToken.None);

            membresia.Activa.Should().BeFalse();
            membresia.Estado.Should().Be("Cancelada");
        }

        [Fact]
        public async Task Handle_MembresiaYaInactiva_NoDeberiaFallarNiSobreescribirElEstado()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var membresia = new MembresiaSocio
            {
                SocioId = 1,
                TipoMembresiaId = 1,
                FechaInicio = DateTime.UtcNow.AddDays(-40),
                FechaFin = DateTime.UtcNow.AddDays(-10),
                Activa = false,
                Estado = "Vencida",
                TenantId = "tenant-1"
            };
            context.MembresiasSocios.Add(membresia);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new CancelarMembresiaCommandHandler(context);
            await handler.Handle(new CancelarMembresiaCommand(membresia.Id), CancellationToken.None);

            // Una membresía ya vencida no debe pasar a decir "Cancelada"
            membresia.Estado.Should().Be("Vencida");
        }

        [Fact]
        public async Task Handle_MembresiaInexistente_DeberiaLanzarKeyNotFoundException()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var handler = new CancelarMembresiaCommandHandler(context);

            var act = async () => await handler.Handle(new CancelarMembresiaCommand(999), CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}
