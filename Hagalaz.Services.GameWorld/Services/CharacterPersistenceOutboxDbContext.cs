using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class CharacterPersistenceOutboxDbContext : DbContext
{
    public CharacterPersistenceOutboxDbContext(DbContextOptions<CharacterPersistenceOutboxDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
