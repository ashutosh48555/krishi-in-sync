using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgriSyncIndia.Data;

/// <summary>Design-time only (dotnet ef). The connection string is a placeholder and is never opened.</summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AgriSyncDbContext>
{
    public AgriSyncDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AgriSyncDbContext>()
            .UseNpgsql("Host=design-time-placeholder;Database=placeholder;Username=placeholder")
            .Options);
}
