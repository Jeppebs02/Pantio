using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PantioRepository.EntityFramework;

public class PantioDbContextFactory : IDesignTimeDbContextFactory<PantioDbContext>
{
    public PantioDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PantioDbContext>()
            .UseNpgsql("Host=localhost;Database=pantio_design_time;Username=postgres")
            .Options;
        return new PantioDbContext(options);
    }
}
