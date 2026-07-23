using FluentAssertions;
using GymSaaS.Application.Reportes.Queries.GetReporteIngresos;
using GymSaaS.Application.Reportes.Queries.GetReporteOcupacionClases;
using GymSaaS.Application.Reportes.Queries.GetReporteSocios;
using GymSaaS.Domain.Entities;
using GymSaaS.UnitTests.TestHelpers;
using Xunit;

namespace GymSaaS.UnitTests.Reportes
{
    public class ReportesTests
    {
        private const string TenantId = "tenant-1";

        [Fact]
        public async Task GetReporteIngresos_DeberiaSumarPagadosYRellenarMesesSinDatosConCero()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService(TenantId));
            var hoy = DateTime.Now;

            context.Pagos.Add(new Pago { Monto = 1000, FechaPago = hoy, Pagado = true, MetodoPago = "Efectivo", SocioId = 1, TenantId = TenantId });
            context.Pagos.Add(new Pago { Monto = 500, FechaPago = hoy, Pagado = true, MetodoPago = "Efectivo", SocioId = 1, TenantId = TenantId });
            context.Pagos.Add(new Pago { Monto = 9999, FechaPago = hoy, Pagado = false, MetodoPago = "Efectivo", SocioId = 1, TenantId = TenantId }); // no cuenta
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new GetReporteIngresosQueryHandler(context);
            var resultado = await handler.Handle(new GetReporteIngresosQuery(3), CancellationToken.None);

            resultado.Should().HaveCount(3);
            resultado.Last().Total.Should().Be(1500); // el mes actual es el último del rango
            resultado.Take(2).Should().OnlyContain(r => r.Total == 0);
        }

        [Fact]
        public async Task GetReporteSocios_DeberiaContarAltasYBajasDelMesCorrecto()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService(TenantId));
            var hoy = DateTime.Now;

            context.Socios.Add(new Socio { Nombre = "A", Apellido = "A", Dni = "1", Email = "a@a.com", FechaAlta = hoy, TenantId = TenantId });
            context.Socios.Add(new Socio { Nombre = "B", Apellido = "B", Dni = "2", Email = "b@b.com", FechaAlta = hoy, TenantId = TenantId });
            await context.SaveChangesAsync(CancellationToken.None);

            context.MembresiasSocios.Add(new MembresiaSocio
            {
                SocioId = 1,
                TipoMembresiaId = 1,
                FechaInicio = hoy.AddDays(-30),
                FechaFin = hoy,
                Activa = false,
                TenantId = TenantId
            });
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new GetReporteSociosQueryHandler(context);
            var resultado = await handler.Handle(new GetReporteSociosQuery(3), CancellationToken.None);

            var mesActual = resultado.Last();
            mesActual.Altas.Should().Be(2);
            mesActual.Bajas.Should().Be(1);
        }

        [Fact]
        public async Task GetReporteOcupacionClases_DeberiaCalcularPorcentajeYPromedio()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService(TenantId));
            var hoy = DateTime.Now;

            var clase = new Clase
            {
                Nombre = "Yoga",
                FechaHoraInicio = hoy,
                DuracionMinutos = 60,
                CupoMaximo = 10,
                TenantId = TenantId
            };
            context.Clases.Add(clase);
            await context.SaveChangesAsync(CancellationToken.None);

            context.Reservas.Add(new Reserva { ClaseId = clase.Id, SocioId = 1, FechaReserva = hoy, Activa = true, TenantId = TenantId });
            context.Reservas.Add(new Reserva { ClaseId = clase.Id, SocioId = 2, FechaReserva = hoy, Activa = true, TenantId = TenantId });
            context.Reservas.Add(new Reserva { ClaseId = clase.Id, SocioId = 3, FechaReserva = hoy, Activa = false, TenantId = TenantId }); // cancelada, no cuenta
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new GetReporteOcupacionClasesQueryHandler(context);
            var resultado = await handler.Handle(new GetReporteOcupacionClasesQuery(30), CancellationToken.None);

            resultado.Clases.Should().ContainSingle();
            resultado.Clases[0].Reservados.Should().Be(2);
            resultado.Clases[0].PorcentajeOcupacion.Should().Be(20.0);
            resultado.PromedioOcupacion.Should().Be(20.0);
        }
    }
}
