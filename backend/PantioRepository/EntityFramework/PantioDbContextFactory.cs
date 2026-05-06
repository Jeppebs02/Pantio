using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PantioRepository.EntityFramework;

public class PantioDbContextFactory : IDesignTimeDbContextFactory<PantioDbContext>
{
    public PantioDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PantioDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=pantio_dev;Username=pantio;Password=pantio_dev_pass")
            .Options;
        return new PantioDbContext(options);
    }
}
