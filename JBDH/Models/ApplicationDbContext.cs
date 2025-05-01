using Microsoft.EntityFrameworkCore;

namespace JBDH.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Define DbSets for your tables
    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }


    // Optional: Override OnModelCreating to configure relationships, constraints, etc.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the User table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id); // Primary key

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100); // Required field with max length

            entity.Property(u => u.PasswordHash)
                .IsRequired(); // Required field

            entity.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50); // Required field with max length

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100); // Required field with max length

            entity.Property(u => u.CVPath)
                .HasMaxLength(2083); // Optional field with max length (e.g., URL length limit)

            entity.Property(u => u.Organisation)
                .IsRequired(false) // Optional field
                .HasMaxLength(255); // Optional field with max length

            entity.Property(u => u.OrganisationId)
                .IsRequired(false); // Nullable field

            entity.Property(u => u.developerMode)
                .IsRequired(false) // Optional field
                .HasDefaultValue(false); // Default value for boolean field

            entity.Property(u => u.openForHiring)
                .IsRequired(false) // Optional field
                .HasDefaultValue(false); // Default value for boolean field
        });
        // Configure the Friendship relationships
    modelBuilder.Entity<Friendship>()
        .HasOne(f => f.Requester)
        .WithMany()
        .HasForeignKey(f => f.RequesterId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Friendship>()
        .HasOne(f => f.Addressee)
        .WithMany()
        .HasForeignKey(f => f.AddresseeId)
        .OnDelete(DeleteBehavior.Restrict);
    }
}