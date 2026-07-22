using GymSaaS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymSaaS.UnitTests.TestHelpers
{
    public static class TestDbContextFactory
    {
        // Cada test recibe su propia base InMemory (nombre único) para no compartir estado.
        public static ApplicationDbContext Create(FakeCurrentTenantService tenantService)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options, tenantService);
        }
    }
}
