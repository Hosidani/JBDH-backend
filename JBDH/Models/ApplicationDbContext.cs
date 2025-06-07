using Microsoft.EntityFrameworkCore;

namespace JBDH.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Manager> Managers { get; set; }

    public DbSet<Recruiter> Recruiters { get; set; }

    public DbSet<Friendship> Friendships { get; set; }

    public DbSet<Organisation> Organisations { get; set; }

    public DbSet<Job> Jobs { get; set; }

    public DbSet<JobApplication> JobApplications { get; set; }

    public DbSet<DataFlow> DataFlows { get; set; }

    public DbSet<AIResponse> AIResponses { get; set; }

    public DbSet<UserAIResponse> UserAIResponses { get; set; }

    public DbSet<JobAIResponse> JobAIResponses { get; set; }

    public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Job>().ToTable("Jobs");
        modelBuilder.Entity<User>()
            .HasDiscriminator<string>("Role") // Use the "Role" property to distinguish types
            .HasValue<User>("User")
            .HasValue<Manager>("Manager")
            .HasValue<Recruiter>("Recruiter");

        // Configure the User table
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id); // Primary key

            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100); // Required field with max length

            entity.Property(u => u.PasswordHash)
                .IsRequired(); // Required field

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100); // Required field with max length

            entity.Property(u => u.Location)
                .IsRequired(false) // Optional field
                .HasMaxLength(255); // Optional field with max length

            entity.Property(u => u.CVPath)
                .HasMaxLength(2083); // Optional field with max length (e.g., URL length limit)

            entity.Property(u => u.DeveloperMode)
                .IsRequired(false) // Optional field
                .HasDefaultValue(false); // Default value for boolean field

            entity.Property(u => u.OpenForHiring)
                .IsRequired(false) // Optional field
                .HasDefaultValue(false); // Default value for boolean field

            entity.Property(u => u.Id)
                .ValueGeneratedOnAdd();
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
        modelBuilder.Entity<Organisation>(entity =>
        {
            entity.HasKey(o => o.Id); // Primary key

            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(255); // Required field with max length

            // Configure many-to-many relationship for Managers
            entity.HasMany(o => o.Managers)
                .WithMany(m => m.ManagedOrganisations)
                .UsingEntity<Dictionary<string, object>>(
                    "OrganisationManagers",
                    j => j.HasOne<Manager>().WithMany().HasForeignKey("ManagerId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Organisation>().WithMany().HasForeignKey("OrganisationId").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("ManagerId", "OrganisationId");
                        j.ToTable("OrganisationManagers");
                    });

        });

        // Configure the Friendship relationships
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure the Friendship relationships
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

        modelBuilder.Entity<AIResponse>(entity =>
        {
            entity.HasKey(ar => ar.id); // Primary key

            entity.Property(ar => ar.responseText)
                .IsRequired(); // Required field

            entity.Property(ar => ar.createdAt)
                .IsRequired(); // Required field
        });
        modelBuilder.Entity<UserAIResponse>()
            .HasOne(uai => uai.User)
            .WithMany(u => u.AIResponses)
            .HasForeignKey(uai => uai.UserId);

        modelBuilder.Entity<JobAIResponse>(entity =>
        {
            entity.HasKey(jr => jr.Id); // Primary key

            entity.Property(jr => jr.ResponseText)
                .IsRequired();

            entity.Property(jr => jr.CreatedAt)
                .IsRequired();

            entity.Property(jr => jr.JobId)
                .IsRequired();

            // Optional: Add a foreign key constraint if you want referential integrity
            entity.HasOne<Job>()
                .WithMany()
                .HasForeignKey(jr => jr.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserRoleAssignment>()
            .HasKey(ura => new { ura.UserId, ura.Role });

        modelBuilder.Entity<UserRoleAssignment>()
            .HasOne(ura => ura.User)
            .WithMany(u => u.Roles)
            .HasForeignKey(ura => ura.UserId);

        modelBuilder.Entity<User>()
            .HasMany(u => u.OwnedUsers)
            .WithOne(u => u.OwnedByRecruiter)
            .HasForeignKey(u => u.OwnedByRecruiterId);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.Recruiter)
            .WithMany()
            .HasForeignKey(j => j.RecruiterId);
    }
}