using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // <-- add this

namespace Sensor.Service.Data;

public class SensorDbContext : DbContext
{
    public SensorDbContext(DbContextOptions<SensorDbContext> options) : base(options) { }
    public DbSet<SensorReading> Readings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store DateTimeOffset as a sortable 64-bit value in SQLite
        var tsConverter = new DateTimeOffsetToBinaryConverter();

        modelBuilder.Entity<SensorReading>()
            .Property(r => r.Timestamp)
            .HasConversion(tsConverter);

        modelBuilder.Entity<SensorReading>()
            .HasIndex(r => r.Timestamp);
    }
}
