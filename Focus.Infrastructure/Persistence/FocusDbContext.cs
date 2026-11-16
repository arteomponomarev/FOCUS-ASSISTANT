using Focus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Focus.Infrastructure.Persistence;

public sealed class FocusDbContext : DbContext
{
    public FocusDbContext(
        DbContextOptions<FocusDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<FocusSession> FocusSessions =>
        Set<FocusSession>();

    public DbSet<SessionGoal> SessionGoals =>
        Set<SessionGoal>();

    public DbSet<ActivityAggregate> ActivityAggregates =>
        Set<ActivityAggregate>();

    public DbSet<FocusStateRecord> FocusStateRecords =>
    Set<FocusStateRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FocusDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}