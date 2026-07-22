using GymSaaS.Application.Common.Interfaces;

namespace GymSaaS.UnitTests.TestHelpers
{
    public class FakeCurrentTenantService : ICurrentTenantService
    {
        public string? TenantId { get; set; }

        public FakeCurrentTenantService(string? tenantId = "tenant-1")
        {
            TenantId = tenantId;
        }
    }
}
