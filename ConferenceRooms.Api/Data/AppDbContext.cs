using ConferenceRooms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRooms.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<HallAdditionalService> HallServices => Set<HallAdditionalService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingServiceSnapshot> BookingServices => Set<BookingServiceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hall>(entity =>
        {
            entity.ToTable("Halls");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.BaseHourlyRate).HasPrecision(12, 2);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasMany(x => x.Services)
                .WithOne(x => x.Hall)
                .HasForeignKey(x => x.HallId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HallAdditionalService>(entity =>
        {
            entity.ToTable("HallServices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Price).HasPrecision(12, 2);
            entity.HasIndex(x => new { x.HallId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BaseHourlyRateSnapshot).HasPrecision(12, 2);
            entity.Property(x => x.RoomAmount).HasPrecision(12, 2);
            entity.Property(x => x.ServicesAmount).HasPrecision(12, 2);
            entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
            entity.HasIndex(x => new { x.HallId, x.StartAt, x.EndAt });
            entity.HasOne(x => x.Hall)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.HallId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Services)
                .WithOne(x => x.Booking)
                .HasForeignKey(x => x.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingServiceSnapshot>(entity =>
        {
            entity.ToTable("BookingServices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Price).HasPrecision(12, 2);
        });
    }
}
