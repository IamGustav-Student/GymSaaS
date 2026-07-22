using FluentAssertions;
using GymSaaS.Application.Asistencias.Commands.RegistrarIngresoQr;
using GymSaaS.Application.Common.Interfaces;
using GymSaaS.Domain.Entities;
using GymSaaS.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GymSaaS.UnitTests.Asistencias
{
    public class RegistrarIngresoQrCommandTests
    {
        private const string TenantCode = "gym-test";

        private static async Task<(IApplicationDbContext Ctx, Socio Socio)> SeedAsync(
            double tenantLat, double tenantLon, int radioMetros)
        {
            var tenantService = new FakeCurrentTenantService(TenantCode);
            var context = TestDbContextFactory.Create(tenantService);

            var tenant = new Tenant
            {
                Code = TenantCode,
                Name = "Gym Test",
                Latitud = tenantLat,
                Longitud = tenantLon,
                RadioPermitidoMetros = radioMetros,
                CodigoQrGym = "QR-TEST",
                TimeZoneId = "UTC"
            };
            context.Tenants.Add(tenant);

            var tipoMembresia = new TipoMembresia
            {
                Nombre = "Full",
                Precio = 100,
                DuracionDias = 30,
                TenantId = TenantCode,
                AccesoLunes = true,
                AccesoMartes = true,
                AccesoMiercoles = true,
                AccesoJueves = true,
                AccesoViernes = true,
                AccesoSabado = true,
                AccesoDomingo = true
            };
            context.TiposMembresia.Add(tipoMembresia);

            var socio = new Socio
            {
                Nombre = "Juan",
                Apellido = "Perez",
                Dni = "12345678",
                Email = "juan@test.com",
                TenantId = TenantCode
            };
            context.Socios.Add(socio);

            await context.SaveChangesAsync(CancellationToken.None);

            var membresia = new MembresiaSocio
            {
                SocioId = socio.Id,
                TipoMembresiaId = tipoMembresia.Id,
                FechaInicio = DateTime.UtcNow.AddDays(-1),
                FechaFin = DateTime.UtcNow.AddDays(29),
                Activa = true,
                TenantId = TenantCode
            };
            context.MembresiasSocios.Add(membresia);
            await context.SaveChangesAsync(CancellationToken.None);

            return (context, socio);
        }

        private static RegistrarIngresoQrCommandHandler CreateHandler(IApplicationDbContext context) =>
            new(
                context,
                Substitute.For<INotificationService>(),
                Substitute.For<IConfiguration>(),
                Substitute.For<ILogger<RegistrarIngresoQrCommandHandler>>(),
                Substitute.For<IAccesoHubService>());

        [Fact]
        public async Task Handle_SocioDentroDelRadioPermitido_DeberiaPermitirAcceso()
        {
            // Gimnasio en el Obelisco, socio escaneando desde el mismo punto (distancia ~0m)
            var (ctx, socio) = await SeedAsync(-34.6037, -58.3816, radioMetros: 100);
            var handler = CreateHandler(ctx);

            var result = await handler.Handle(new RegistrarIngresoQrCommand
            {
                SocioId = socio.Id,
                CodigoQrEscaneado = "QR-TEST",
                LatitudUsuario = -34.6037,
                LongitudUsuario = -58.3816
            }, CancellationToken.None);

            result.Exitoso.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_SocioFueraDelRadioPermitido_DeberiaRechazarPorDistancia()
        {
            // Gimnasio en el Obelisco (radio 100m), socio escaneando a ~900m de distancia
            var (ctx, socio) = await SeedAsync(-34.6037, -58.3816, radioMetros: 100);
            var handler = CreateHandler(ctx);

            var result = await handler.Handle(new RegistrarIngresoQrCommand
            {
                SocioId = socio.Id,
                CodigoQrEscaneado = "QR-TEST",
                LatitudUsuario = -34.6100,
                LongitudUsuario = -58.3900
            }, CancellationToken.None);

            result.Exitoso.Should().BeFalse();
            result.Mensaje.Should().Contain("lejos");
        }

        [Fact]
        public async Task Handle_CodigoQrIncorrecto_DeberiaRechazarAunEstandoCerca()
        {
            var (ctx, socio) = await SeedAsync(-34.6037, -58.3816, radioMetros: 100);
            var handler = CreateHandler(ctx);

            var result = await handler.Handle(new RegistrarIngresoQrCommand
            {
                SocioId = socio.Id,
                CodigoQrEscaneado = "QR-DE-OTRO-GYM",
                LatitudUsuario = -34.6037,
                LongitudUsuario = -58.3816
            }, CancellationToken.None);

            result.Exitoso.Should().BeFalse();
            result.Mensaje.Should().Contain("Código QR inválido");
        }

        [Fact]
        public async Task Handle_SinCoordenadas_OmiteGeofencingYUsaSoloMembresia()
        {
            // Ingreso manual desde recepción (lat/lon en 0): no debe aplicar geofencing
            var (ctx, socio) = await SeedAsync(-34.6037, -58.3816, radioMetros: 100);
            var handler = CreateHandler(ctx);

            var result = await handler.Handle(new RegistrarIngresoQrCommand
            {
                SocioId = socio.Id,
                CodigoQrEscaneado = "",
                LatitudUsuario = 0,
                LongitudUsuario = 0
            }, CancellationToken.None);

            result.Exitoso.Should().BeTrue();
        }
    }
}
