using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Brenda.Infrastructure.Data;

/// <summary>Used only by the dotnet-ef tooling to create migrations.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BrendaDbContext>
{
    public BrendaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BrendaDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;

        return new BrendaDbContext(options);
    }
}
