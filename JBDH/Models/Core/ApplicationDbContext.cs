using JBDH.Models.User;
using JBDH.Models.Job;
using JBDH.Models.AI;
using JBDH.Models.Dataflow;
using Microsoft.EntityFrameworkCore;
using FriendshipModel = JBDH.Models.Friendship.Friendship;
using JBDH.Models.Authentication;
using JBDH.Models.Notifications;
using JBDH.Models.Organisation;
using JBDH.Models.Kanban;
using JBDH.Models.Messaging; 

namespace JBDH.Models.Core
{
    public class ApplicationDbContext : DbContext
        
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

        public DbSet<JBDH.Models.User.User> Users { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Recruiter> Recruiters { get; set; }

        public DbSet<FriendshipModel> Friendships { get; set; }

        public DbSet<JBDH.Models.Organisation.Organisation> Organisations { get; set; }

        public DbSet<RawJob> RawJobs { get; set; } 

        public DbSet<JBDH.Models.Job.Job> Jobs { get; set; }

        public DbSet<JobApplication> JobApplications { get; set; }

        public DbSet<DataFlow> DataFlows { get; set; }

        public DbSet<AIResponse> AIResponses { get; set; }

        public DbSet<UserAIResponse> UserAIResponses { get; set; }

        public DbSet<JobAIResponse> JobAIResponses { get; set; }

        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<PasswordReset> PasswordResets { get; set; }

        public DbSet<JBDH.Models.Kanban.Board> Boards { get; set; }
        public DbSet<JBDH.Models.Kanban.KanbanList> Lists { get; set; }
        public DbSet<JBDH.Models.Kanban.Card> Cards { get; set; }
        public DbSet<JBDH.Models.Kanban.BoardMembership> BoardMemberships { get; set; }
        public DbSet<JBDH.Models.Kanban.Organization> Organizations { get; set; }

        public DbSet<Message> Messages { get; set; } 
        public DbSet<AIUserCourseProposal> AIUserCourseProposals { get; set; }
        public DbSet<JBDH.Models.Job.SavedJob> SavedJobs { get; set; }
        public DbSet<JBDH.Models.Job.NotInterestedJob> NotInterestedJobs { get; set; } 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<JBDH.Models.Job.Job>().ToTable("Jobs");
            modelBuilder.Entity<RawJob>().ToTable("rawjobs");
            modelBuilder.Entity<JBDH.Models.User.User>()
                .HasDiscriminator<string>("Role") 
                .HasValue<JBDH.Models.User.User>("User")
                .HasValue<Manager>("Manager")
                .HasValue<Recruiter>("Recruiter");

            // Configure the User table
            modelBuilder.Entity<JBDH.Models.User.User>(entity =>
            {
                entity.HasKey(u => u.Id); // Primary key

                entity.Property(u => u.PasswordHash)
                    .IsRequired(); // Required field

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100); 

                entity.Property(u => u.Location)
                    .IsRequired(false) // Optional field
                    .HasMaxLength(255); // Optional field with max length

                entity.Property(u => u.CVPath)
                    .HasMaxLength(2083); // Optional field with max length (e.g., URL length limit)

                entity.Property(u => u.DeveloperMode)
                    .IsRequired(false) // Optional field
                    .HasDefaultValue(false); // Default value for boolean field

                entity.Property(u => u.Searchable)
                    .IsRequired(false)
                    .HasDefaultValue(false); // Default value for boolean field

                entity.Property(u => u.Id)
                    .ValueGeneratedOnAdd();
            });

            // Configure the Account table
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id); // Primary key

                entity.Property(a => a.AccountName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(a => a.AccountEmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(a => a.AccountPhone)
                    .HasMaxLength(20);

                entity.Property(a => a.PreferredLanguage)
                    .HasMaxLength(10)
                    .HasDefaultValue("en");

                entity.Property(a => a.AccountType)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .HasDefaultValue(AccountType.Basic);

                entity.Property(a => a.IsTerminated)
                    .HasDefaultValue(false);

                entity.Property(a => a.AcceptedTermsAndConditions)
                    .HasDefaultValue(false);

                entity.Property(a => a.CreatedAt)
                    .IsRequired();

                // Configure relationship with User
                entity.HasOne(a => a.User)
                    .WithMany(u => u.Accounts)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
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
            modelBuilder.Entity<JBDH.Models.Organisation.Organisation>(entity =>
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

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Recipient)
                    .WithMany()
                    .HasForeignKey(m => m.RecipientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(m => m.Content)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(m => m.SentAt)
                    .IsRequired();
            });

            modelBuilder.Entity<AIUserCourseProposal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProposalJson).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SavedJob relationships
            modelBuilder.Entity<JBDH.Models.Job.SavedJob>(entity =>
            {
                entity.HasKey(sj => sj.Id); // Primary key

                entity.Property(sj => sj.Notes)
                    .HasMaxLength(1000); // Optional field with max length

                entity.Property(sj => sj.SavedAt)
                    .IsRequired(); // Required field

                // Configure relationship with User
                entity.HasOne(sj => sj.User)
                    .WithMany() // User has many SavedJobs
                    .HasForeignKey(sj => sj.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // Cascade delete: if User is deleted -> SavedJob is deleted

                // Configure relationship with Job
                entity.HasOne(sj => sj.Job)
                    .WithMany() // Job has many SavedJobs
                    .HasForeignKey(sj => sj.JobId)
                    .OnDelete(DeleteBehavior.Cascade); // Cascade delete: if Job is deleted -> SavedJob is deleted

                // Add unique constraint to prevent duplicate saves
                entity.HasIndex(sj => new { sj.UserId, sj.JobId })
                    .IsUnique();
            });

            // NotInterestedJob configuration
            modelBuilder.Entity<NotInterestedJob>()
                .HasOne(nij => nij.User)
                .WithMany()
                .HasForeignKey(nij => nij.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotInterestedJob>()
                .HasOne(nij => nij.Job)
                .WithMany()
                .HasForeignKey(nij => nij.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint - en användare kan bara markera samma jobb en gång
            modelBuilder.Entity<NotInterestedJob>()
                .HasIndex(nij => new { nij.UserId, nij.JobId })
                .IsUnique();
        }
    }
}