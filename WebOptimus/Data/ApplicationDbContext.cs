using WebOptimus.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


namespace WebOptimus.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<Contact> Contacts => Set<Contact>();
   
        public DbSet<Audit> Audits => Set<Audit>();
               public DbSet<PasswordReset> PasswordResets => Set<PasswordReset>();

        public DbSet<PageNavigationLog> PageNavigationLogs => Set<PageNavigationLog>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<VoteCast> VoteCasts => Set<VoteCast>();

        public DbSet<ImpersonationLog> ImpersonationLogs => Set<ImpersonationLog>();
        public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();
        public DbSet<History> History => Set<History>();
        public DbSet<Dependant> Dependants  => Set<Dependant>();
        public DbSet<UserNotification> UserNotification => Set<UserNotification>();
        
        public DbSet<DependentChecklistItem> DependentChecklistItems { get; set; }
        
              public DbSet<SeparationHistory> SeparationHistory => Set<SeparationHistory>();

        public DbSet<Settings> Settings => Set<Settings>();
        public DbSet<Cause> Cause => Set<Cause>();
        public DbSet<PopUpNotification> PopUpNotification => Set<PopUpNotification>();
        public DbSet<Successor> Successors => Set<Successor>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<ReportedDeath> ReportedDeath => Set<ReportedDeath>();
        public DbSet<NextOfKin> NextOfKins => Set<NextOfKin>();

        public DbSet<Payment> Payment => Set<Payment>();
        public DbSet<OtherDonationPayment> OtherDonationPayment => Set<OtherDonationPayment>();
        
        public DbSet<PaymentSession> PaymentSessions => Set<PaymentSession>();
        public DbSet<Title> Title => Set<Title>();
        public DbSet<Region> Region => Set<Region>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<DependentChangeLog> DependentChangeLogs => Set<DependentChangeLog>();
        public DbSet<UserProfileChangeLog> UserProfileChangeLog => Set<UserProfileChangeLog>();
        public DbSet<ReportedDeathChangeLog> ReportedDeathChangeLog => Set<ReportedDeathChangeLog>();
        
        public DbSet<NextOfKinChangeLog> NextOfKinChangeLogs => Set<NextOfKinChangeLog>();
        public DbSet<City> City => Set<City>();
        public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
        public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
        public DbSet<Constitution> Constitution => Set<Constitution>();
        public DbSet<Election> Elections => Set<Election>();
        public DbSet<Gender> Gender => Set<Gender>();
        //public DbSet<OtherDonation> OtherDonation => Set<OtherDonation>();
        public DbSet<Poll> Poll => Set<Poll>();
        public DbSet<PollResponse> PollResponse => Set<PollResponse>();
        public DbSet<PollOption> PollOptions => Set<PollOption>(); 
        public DbSet<ConfirmedDeath> ConfirmedDeath => Set<ConfirmedDeath>();

        public DbSet<DeletedUsers> DeletedUser => Set<DeletedUsers>();

        public DbSet<DonationForNonDeathRelated> DonationForNonDeathRelated => Set<DonationForNonDeathRelated>();
        public DbSet<PaymentGroup> Groups => Set<PaymentGroup>();
        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
        public DbSet<UserLoginSession> UserLoginSessions => Set<UserLoginSession>();

        public DbSet<ScheduledTask> ScheduledTask => Set<ScheduledTask>();

        public DbSet<NoteType> NoteTypes => Set<NoteType>();

        public DbSet<NoteHistory> NoteHistory => Set<NoteHistory>();
        public DbSet<RequestToCancelMembership> RequestToCancelMembership => Set<RequestToCancelMembership>();


        public DbSet<NoteChangeLogs> NoteChangeLogs => Set<NoteChangeLogs>();
        public DbSet<CustomPayment> CustomPayment => Set<CustomPayment>();

        public DbSet<ScheduledStoredProcedure> ScheduledStoredProcedure => Set<ScheduledStoredProcedure>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<Group>();

            modelBuilder.Entity<PaymentSession>()
             .HasMany(p => p.DependentsChecklist)
             .WithOne()
             .HasForeignKey(d => d.PaymentSessionId);

            modelBuilder.Entity<NoteType>().HasData(
           new NoteType { Id = 1, TypeName = "Account", DateCreated = DateTime.UtcNow },
           new NoteType { Id = 2, TypeName = "Complaints", DateCreated = DateTime.UtcNow },
           new NoteType { Id = 3, TypeName = "Payments", DateCreated = DateTime.UtcNow },
           new NoteType { Id = 4, TypeName = "General", DateCreated = DateTime.UtcNow }
       );

        }
    }
}