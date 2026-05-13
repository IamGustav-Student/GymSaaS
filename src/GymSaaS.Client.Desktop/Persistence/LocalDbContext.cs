using Microsoft.EntityFrameworkCore;

namespace GymSaaS.Client.Desktop.Persistence
{
    public class LocalSocio
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public string Estado { get; set; } = "Activo";
        public string? UltimoAcceso { get; set; }
    }

    public class LocalSetting
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class LocalDbContext : DbContext
    {
        public DbSet<LocalSocio> Socios => Set<LocalSocio>();
        public DbSet<LocalSetting> Settings => Set<LocalSetting>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=gymvo_local.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalSocio>().HasKey(s => s.Id);
            modelBuilder.Entity<LocalSetting>().HasKey(s => s.Key);
        }
    }
}
