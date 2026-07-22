using FluentAssertions;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Application.Pagos.Commands.EjecutarReintentos;
using GymSaaS.Domain.Entities;
using GymSaaS.UnitTests.TestHelpers;
using NSubstitute;
using Xunit;

namespace GymSaaS.UnitTests.Pagos
{
    // Cubre el fix del Sprint 1: este job antes tenía "bool cobroExitoso = true; // Simulación"
    // y marcaba pagos como cobrados sin cobrar nada realmente. Ahora nunca debe poner
    // Pagado = true — solo debe recordarle la deuda al socio y contar los intentos.
    public class EjecutarReintentosCommandTests
    {
        private static Socio CrearSocio(string tenantId) => new()
        {
            Nombre = "Ana",
            Apellido = "Gomez",
            Dni = "1111111",
            Email = "ana@test.com",
            Telefono = "+5491100000000",
            TenantId = tenantId
        };

        [Fact]
        public async Task Handle_PagoConReintentoPendiente_NuncaDeberiaMarcarloComoPagado()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var socio = CrearSocio("tenant-1");
            context.Socios.Add(socio);
            await context.SaveChangesAsync(CancellationToken.None);

            var pago = new Pago
            {
                SocioId = socio.Id,
                Monto = 15000,
                MetodoPago = "TarjetaCredito",
                Pagado = false,
                EstadoTransaccion = "rejected_insufficient_funds",
                IntentosFallidos = 0,
                ProximoReintento = DateTime.UtcNow.AddDays(-1), // ya venció el plazo de reintento
                TenantId = "tenant-1"
            };
            context.Pagos.Add(pago);
            await context.SaveChangesAsync(CancellationToken.None);

            var notificationService = Substitute.For<INotificationService>();
            var handler = new EjecutarReintentosCommandHandler(
                context, notificationService, Substitute.For<Microsoft.Extensions.Logging.ILogger<EjecutarReintentosCommandHandler>>());

            var procesados = await handler.Handle(new EjecutarReintentosCommand(), CancellationToken.None);

            procesados.Should().Be(1);
            pago.Pagado.Should().BeFalse("el job no puede cobrar de verdad todavía, solo recordar la deuda");
            pago.IntentosFallidos.Should().Be(1);
            pago.ProximoReintento.Should().NotBeNull();
            await notificationService.Received(1).EnviarAlertaPagoFallido(socio.Nombre, socio.Telefono!, Arg.Any<DateTime>());
        }

        [Fact]
        public async Task Handle_TercerIntentoFallido_DeberiaMarcarComoFinalFailedYDejarDeReintentar()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var socio = CrearSocio("tenant-1");
            context.Socios.Add(socio);
            await context.SaveChangesAsync(CancellationToken.None);

            var pago = new Pago
            {
                SocioId = socio.Id,
                Monto = 15000,
                MetodoPago = "TarjetaCredito",
                Pagado = false,
                IntentosFallidos = 2, // este sería el 3er intento
                ProximoReintento = DateTime.UtcNow.AddDays(-1),
                TenantId = "tenant-1"
            };
            context.Pagos.Add(pago);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new EjecutarReintentosCommandHandler(
                context, Substitute.For<INotificationService>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<EjecutarReintentosCommandHandler>>());

            await handler.Handle(new EjecutarReintentosCommand(), CancellationToken.None);

            pago.Pagado.Should().BeFalse();
            pago.IntentosFallidos.Should().Be(3);
            pago.EstadoTransaccion.Should().Be("final_failed");
            pago.ProximoReintento.Should().BeNull();
        }

        [Fact]
        public async Task Handle_PagoConReintentoFuturo_NoDeberiaProcesarloTodavia()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService());
            var socio = CrearSocio("tenant-1");
            context.Socios.Add(socio);
            await context.SaveChangesAsync(CancellationToken.None);

            var pago = new Pago
            {
                SocioId = socio.Id,
                Monto = 15000,
                MetodoPago = "TarjetaCredito",
                Pagado = false,
                IntentosFallidos = 0,
                ProximoReintento = DateTime.UtcNow.AddDays(3), // todavía no toca reintentar
                TenantId = "tenant-1"
            };
            context.Pagos.Add(pago);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new EjecutarReintentosCommandHandler(
                context, Substitute.For<INotificationService>(), Substitute.For<Microsoft.Extensions.Logging.ILogger<EjecutarReintentosCommandHandler>>());

            var procesados = await handler.Handle(new EjecutarReintentosCommand(), CancellationToken.None);

            procesados.Should().Be(0);
            pago.IntentosFallidos.Should().Be(0);
        }
    }
}
