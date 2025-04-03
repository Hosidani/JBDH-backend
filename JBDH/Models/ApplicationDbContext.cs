using Microsoft.EntityFrameworkCore;

namespace JBDH.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Define DbSets for your tables
    public DbSet<User> Users { get; set; }

    // Optional: Override OnModelCreating to configure relationships, constraints, etc.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Example: Configure the User table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id); // Primary key
            entity.Property(u => u.Username).IsRequired().HasMaxLength(100); // Required field with max length
            entity.Property(u => u.PasswordHash).IsRequired(); // Required field
        });
    }
}
