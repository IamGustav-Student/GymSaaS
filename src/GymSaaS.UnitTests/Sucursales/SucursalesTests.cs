using FluentAssertions;
using GymSaaS.Application.Sucursales.Commands.CrearSucursal;
using GymSaaS.Application.Sucursales.Queries.GetMisSucursales;
using GymSaaS.Application.Sucursales.Queries.GetResumenEmpresa;
using GymSaaS.Application.Sucursales.Queries.PrepararCambioDeSucursal;
using GymSaaS.Domain.Entities;
using GymSaaS.Infrastructure.Persistence;
using GymSaaS.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymSaaS.UnitTests.Sucursales
{
    public class SucursalesTests
    {
        private static async Task<(ApplicationDbContext Context, Tenant TenantPrincipal, Usuario Admin, Empresa Empresa)> SeedGymConAdminAsync()
        {
            var context = TestDbContextFactory.Create(new FakeCurrentTenantService(null));

            var empresa = new Empresa { Nombre = "Cadena Test" };
            context.Empresas.Add(empresa);
            await context.SaveChangesAsync(CancellationToken.None);

            var tenant = new Tenant { Name = "Sede Centro", Code = "sede-centro", EmpresaId = empresa.Id };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync(CancellationToken.None);

            var admin = new Usuario
            {
                Nombre = "Admin",
                Email = "admin@test.com",
                Password = "hash-fake",
                Activo = true,
                Role = "Admin",
                TenantId = tenant.Id.ToString()
            };
            context.Usuarios.Add(admin);
            await context.SaveChangesAsync(CancellationToken.None);

            return (context, tenant, admin, empresa);
        }

        [Fact]
        public async Task CrearSucursal_DeberiaCrearNuevoTenantEnLaMismaEmpresaYClonarElAdmin()
        {
            var (context, tenant, admin, empresa) = await SeedGymConAdminAsync();
            var tenantService = new FakeCurrentTenantService(tenant.Id.ToString());
            var handler = new CrearSucursalCommandHandler(context, tenantService);

            var nuevoTenantId = await handler.Handle(new CrearSucursalCommand
            {
                UsuarioActualId = admin.Id,
                NombreSucursal = "Sede Norte"
            }, CancellationToken.None);

            var nuevoTenant = context.Tenants.First(t => t.Id == nuevoTenantId);
            nuevoTenant.EmpresaId.Should().Be(empresa.Id);
            nuevoTenant.Name.Should().Be("Sede Norte");

            var adminClonado = context.Usuarios.IgnoreQueryFilters().First(u => u.TenantId == nuevoTenantId.ToString());
            adminClonado.Email.Should().Be(admin.Email);
            adminClonado.Password.Should().Be(admin.Password);
            adminClonado.Role.Should().Be("Admin");
        }

        [Fact]
        public async Task CrearSucursal_ConUsuarioDeOtroTenant_DeberiaRechazar()
        {
            var (context, tenant, _, _) = await SeedGymConAdminAsync();
            var tenantService = new FakeCurrentTenantService(tenant.Id.ToString());
            var handler = new CrearSucursalCommandHandler(context, tenantService);

            var act = async () => await handler.Handle(new CrearSucursalCommand
            {
                UsuarioActualId = 9999, // no existe / no pertenece a este tenant
                NombreSucursal = "Sede Trucha"
            }, CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task GetMisSucursales_DeberiaListarTodasLasSucursalesDeLaEmpresa()
        {
            var (context, tenant, admin, empresa) = await SeedGymConAdminAsync();

            var otraSede = new Tenant { Name = "Sede Norte", Code = "sede-norte", EmpresaId = empresa.Id };
            context.Tenants.Add(otraSede);
            await context.SaveChangesAsync(CancellationToken.None);

            var tenantService = new FakeCurrentTenantService(tenant.Id.ToString());
            var handler = new GetMisSucursalesQueryHandler(context, tenantService);

            var sucursales = await handler.Handle(new GetMisSucursalesQuery(), CancellationToken.None);

            sucursales.Should().HaveCount(2);
            sucursales.Single(s => s.Id == tenant.Id).EsLaActual.Should().BeTrue();
            sucursales.Single(s => s.Id == otraSede.Id).EsLaActual.Should().BeFalse();
        }

        [Fact]
        public async Task PrepararCambioDeSucursal_MismoEmailYMismaEmpresa_DeberiaAutorizar()
        {
            var (context, tenant, admin, empresa) = await SeedGymConAdminAsync();

            var otraSede = new Tenant { Name = "Sede Norte", Code = "sede-norte", EmpresaId = empresa.Id };
            context.Tenants.Add(otraSede);
            await context.SaveChangesAsync(CancellationToken.None);

            var adminClonado = new Usuario
            {
                Nombre = admin.Nombre,
                Email = admin.Email,
                Password = admin.Password,
                Activo = true,
                Role = "Admin",
                TenantId = otraSede.Id.ToString()
            };
            context.Usuarios.Add(adminClonado);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new PrepararCambioDeSucursalQueryHandler(context);

            var resultado = await handler.Handle(
                new PrepararCambioDeSucursalQuery(tenant.Id, otraSede.Id, admin.Email), CancellationToken.None);

            resultado.Should().NotBeNull();
            resultado!.TenantId.Should().Be(otraSede.Id.ToString());
            resultado.UsuarioId.Should().Be(adminClonado.Id);
        }

        [Fact]
        public async Task PrepararCambioDeSucursal_TenantDeOtraEmpresa_DeberiaRechazar()
        {
            var (context, tenant, admin, _) = await SeedGymConAdminAsync();

            var otraEmpresa = new Empresa { Nombre = "Otra Cadena" };
            context.Empresas.Add(otraEmpresa);
            await context.SaveChangesAsync(CancellationToken.None);

            var tenantAjeno = new Tenant { Name = "Gym de Otro Dueño", Code = "gym-ajeno", EmpresaId = otraEmpresa.Id };
            context.Tenants.Add(tenantAjeno);
            await context.SaveChangesAsync(CancellationToken.None);

            // Aunque por coincidencia exista un admin con el mismo email en el tenant ajeno...
            var adminAjeno = new Usuario
            {
                Nombre = "Otro Admin",
                Email = admin.Email,
                Password = "otro-hash",
                Activo = true,
                Role = "Admin",
                TenantId = tenantAjeno.Id.ToString()
            };
            context.Usuarios.Add(adminAjeno);
            await context.SaveChangesAsync(CancellationToken.None);

            var handler = new PrepararCambioDeSucursalQueryHandler(context);

            // ...no debería poder "cambiarse" porque no es la misma Empresa
            var resultado = await handler.Handle(
                new PrepararCambioDeSucursalQuery(tenant.Id, tenantAjeno.Id, admin.Email), CancellationToken.None);

            resultado.Should().BeNull();
        }

        [Fact]
        public async Task GetResumenEmpresa_DeberiaSumarSociosActivosEIngresosDeTodasLasSucursales()
        {
            var (context, tenant, _, empresa) = await SeedGymConAdminAsync();

            var otraSede = new Tenant { Name = "Sede Norte", Code = "sede-norte", EmpresaId = empresa.Id };
            context.Tenants.Add(otraSede);
            await context.SaveChangesAsync(CancellationToken.None);

            context.Socios.Add(new Socio { Nombre = "A", Apellido = "A", Dni = "1", Email = "a@a.com", Activo = true, TenantId = tenant.Id.ToString() });
            context.Socios.Add(new Socio { Nombre = "B", Apellido = "B", Dni = "2", Email = "b@b.com", Activo = true, TenantId = otraSede.Id.ToString() });
            await context.SaveChangesAsync(CancellationToken.None);

            context.Pagos.Add(new Pago { Monto = 1000, FechaPago = DateTime.Now, Pagado = true, MetodoPago = "Efectivo", SocioId = 1, TenantId = tenant.Id.ToString() });
            context.Pagos.Add(new Pago { Monto = 2000, FechaPago = DateTime.Now, Pagado = true, MetodoPago = "Efectivo", SocioId = 1, TenantId = otraSede.Id.ToString() });
            await context.SaveChangesAsync(CancellationToken.None);

            var tenantService = new FakeCurrentTenantService(tenant.Id.ToString());
            var handler = new GetResumenEmpresaQueryHandler(context, tenantService);

            var resumen = await handler.Handle(new GetResumenEmpresaQuery(), CancellationToken.None);

            resumen.CantidadSucursales.Should().Be(2);
            resumen.SociosActivosTotal.Should().Be(2);
            resumen.IngresosMensualesTotal.Should().Be(3000);
        }
    }
}
