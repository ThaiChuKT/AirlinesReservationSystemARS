using Microsoft.EntityFrameworkCore;
using ARS.Models;

namespace ARS.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    // public DbSet<Reservation> Reservations { get; set; }
    // public DbSet<Payment> Payments { get; set; }
    // public DbSet<Refund> Refunds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configure Flight entity
        modelBuilder.Entity<Flight>()
            .HasOne(f => f.OriginCity)
            .WithMany(c => c.OriginFlights)
            .HasForeignKey(f => f.OriginCityID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Flight>()
            .HasOne(f => f.DestinationCity)
            .WithMany(c => c.DestinationFlights)
            .HasForeignKey(f => f.DestinationCityID)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Schedule entity
        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Flight)
            .WithMany(f => f.Schedules)
            .HasForeignKey(s => s.FlightID)
            .OnDelete(DeleteBehavior.Cascade);

        // modelBuilder.Entity<User>()
        //     .HasMany(u => u.Reservations)
        //     .WithOne()
        //     .HasForeignKey("UserId");
    }
}