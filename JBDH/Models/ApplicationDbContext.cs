using Microsoft.EntityFrameworkCore;

namespace JBDH.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    // Define DbSets for your tables
    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Manager> Managers { get; set; }

    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }
    public DbSet<DataFlow> DataFlows { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Job>().ToTable("clean_table");
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("Role") // Use the "Role" property to distinguish types
            .HasValue<User>("User")
            .HasValue<Manager>("Manager");

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

        // Configure EmailVerificationToken
        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id); // Define primary key
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // Foreign key to Users table
        });


        // Configure the Organization table
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id); // Primary key

            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(255); // Required field with max length

            // Configure many-to-many relationship for Managers
            entity.HasMany(o => o.Managers)
                .WithMany("ManagedOrganizations") // Directly reference ManagedOrganizations
                .UsingEntity<Dictionary<string, object>>(
                    "OrganizationManagers",
                    j => j.HasOne<User>() // Reference Manager
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

        modelBuilder.Entity<JobApplication>(entity =>
        {
            // Composite Primary Key
            entity.HasKey(ja => new { ja.UserId, ja.JobId });

            // Ensure JobId matches VARCHAR(255)
            entity.Property(ja => ja.JobId)
                .HasMaxLength(255)
                .IsRequired();

            // Optional: explicitly set the table name
            entity.ToTable("JobApplications");
        });
        
        modelBuilder.Entity<DataFlow>(entity =>
        {
            entity.HasKey(df => df.Id); // Primary key

            entity.Property(df => df.ContentType)
                .IsRequired()
                .HasMaxLength(50); // Required field with max length

            entity.Property(df => df.Content)
                .IsRequired(); // Required field

            entity.Property(df => df.CreatedAt)
                .IsRequired(); // Required field
        });

    }
}