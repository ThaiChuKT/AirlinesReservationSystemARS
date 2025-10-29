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
    // public DbSet<Flight> Flights { get; set; }
    // public DbSet<Reservation> Reservations { get; set; }
    // public DbSet<Payment> Payments { get; set; }
    // public DbSet<Refund> Refunds { get; set; }
    // public DbSet<City> Cities { get; set; }
    // public DbSet<Schedule> Schedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // modelBuilder.Entity<User>()
        //     .HasMany(u => u.Reservations)
        //     .WithOne()
        //     .HasForeignKey("UserId");
    }
}