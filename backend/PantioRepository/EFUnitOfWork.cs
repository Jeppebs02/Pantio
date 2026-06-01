using Microsoft.EntityFrameworkCore;
using PantioClassLibrary.Interfaces;
using PantioRepository.EntityFramework;

namespace PantioRepository;

public class EFUnitOfWork(PantioDbContext db) : IUnitOfWork
{
    public Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                await operation();
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}
