using Microsoft.EntityFrameworkCore;

namespace JBDH.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Define DbSets for your tables
    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Manager> Managers { get; set; }

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

            entity.Property(u => u.DeveloperMode)
                .IsRequired(false) // Optional field
                .HasDefaultValue(false); // Default value for boolean field

            entity.Property(u => u.OpenForHiring)
                .IsRequired(false) // Optional field
                .HasDefaultValue(false); // Default value for boolean field
        });

        // Configure the Organization table
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id); // Primary key

            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(255); // Required field with max length

            entity.Property(o => o.Description)
                .HasMaxLength(1000); // Optional field with max length

            // Configure many-to-many relationship for Managers
            entity.HasMany(o => o.Managers)
                .WithMany(m => m.ManagedOrganizations) // Reference Manager, not User
                .UsingEntity<Dictionary<string, object>>(
                    "OrganizationManagers",
                    j => j.HasOne<Manager>() // Reference Manager
                          .WithMany()
                          .HasForeignKey("ManagerId")
                          .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Organization>()
                          .WithMany()
                          .HasForeignKey("OrganizationId")
                          .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("ManagerId", "OrganizationId"); // Composite key
                        j.ToTable("OrganizationManagers");
                    });
        });

        // Configure the Friendship relationships
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Addressee)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}