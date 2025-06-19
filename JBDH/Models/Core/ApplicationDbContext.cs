using JBDH.Models.User;
using JBDH.Models.Job;
using JBDH.Models.AI;
using JBDH.Models.Dataflow;
using Microsoft.EntityFrameworkCore;
using FriendshipModel = JBDH.Models.Friendship.Friendship;

namespace JBDH.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public DbSet<JBDH.Models.User.User> Users { get; set; }

    public DbSet<Recruiter> Recruiters { get; set; }

    public DbSet<FriendshipModel> Friendships { get; set; }

    public DbSet<Organisation> Organisations { get; set; }

    public DbSet<JBDH.Models.Job.Job> Jobs { get; set; }

    public DbSet<JobApplication> JobApplications { get; set; }

    public DbSet<DataFlow> DataFlows { get; set; }

    public DbSet<AIResponse> AIResponses { get; set; }

    public DbSet<UserAIResponse> UserAIResponses { get; set; }

    public DbSet<JobAIResponse> JobAIResponses { get; set; }

    public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
    public DbSet<UserReport> UserReports { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<JBDH.Models.Job.Job>().ToTable("Jobs");
        modelBuilder.Entity<JBDH.Models.User.User>()
            .HasDiscriminator<string>("Role") // Use the "Role" property to distinguish types
            .HasValue<JBDH.Models.User.User>("User")
            .HasValue<Manager>("Manager")
            .HasValue<Recruiter>("Recruiter");

        // Configure the User table
        modelBuilder.Entity<JBDH.Models.User.User>(entity =>
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
            entity.HasOne<JBDH.Models.User.User>()
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
                .WithMany(u => u.ManagedOrganisations)
                .UsingEntity(j => j.ToTable("OrganisationManagers"));
        });

        // Configure the Friendship relationships
        modelBuilder.Entity<FriendshipModel>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure the Friendship relationships
        modelBuilder.Entity<FriendshipModel>()
            .HasOne(f => f.Addressee)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<JobApplication>(entity =>
        {
            // Composite Primary Key
            entity.HasKey(ja => new { ja.UserId, ja.JobId });

            // Remove .HasMaxLength(255) for JobId
            entity.Property(ja => ja.JobId).IsRequired();

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

            entity.HasMany(df => df.RelatedDataFlows)
                .WithOne(df => df.ParentDataFlow)
                .HasForeignKey(df => df.ParentDataFlowId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AIResponse>(entity =>
        {
            entity.HasKey(ar => ar.Id); // Primary key

            entity.Property(ar => ar.ResponseText)
                .IsRequired(); // Required field

            entity.Property(ar => ar.CreatedAt)
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
            entity.HasOne<JBDH.Models.Job.Job>()
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

        modelBuilder.Entity<JBDH.Models.Job.Job>()
            .HasOne(j => j.Recruiter)
            .WithMany()
            .HasForeignKey(j => j.RecruiterId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserReport>(entity =>
        {
            entity.HasKey(r => r.Id); // Primary key

            entity.Property(r => r.Reason)
                .IsRequired(); // Required field

            entity.Property(r => r.CreatedAt)
                .IsRequired(); // Required field
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(pr => pr.Id); // Primary key

            entity.Property(pr => pr.Token)
                .IsRequired()
                .HasMaxLength(255); // Required field with max length

            entity.Property(pr => pr.ExpirationDate)
                .IsRequired(); // Required field

            entity.HasOne(pr => pr.User)
                .WithMany()
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Foreign key to Users table
        });

        // Manager owns Recruiters
        modelBuilder.Entity<Manager>()
            .HasMany(m => m.OwnedRecruiters)
            .WithOne()
            .HasForeignKey(r => r.OwnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Job owned by Manager or Recruiter
        modelBuilder.Entity<JBDH.Models.Job.Job>()
            .HasOne(j => j.Owner)
            .WithMany()
            .HasForeignKey(j => j.OwnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // User owns Users (for both Manager and Recruiter)
        modelBuilder.Entity<JBDH.Models.User.User>()
            .HasMany(u => u.OwnedUsers)
            .WithOne(u => u.Owner)
            .HasForeignKey(u => u.OwnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}