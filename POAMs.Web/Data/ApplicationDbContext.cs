using Microsoft.EntityFrameworkCore;
using POAMs.Web.Models.Domain;

namespace POAMs.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<SystemInfo> Systems => Set<SystemInfo>();
    public DbSet<POAM> POAMs => Set<POAM>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<UserAssignment> UserAssignments => Set<UserAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SecurityTestPlan> SecurityTestPlans => Set<SecurityTestPlan>();
    public DbSet<STPTestCase> STPTestCases => Set<STPTestCase>();
    public DbSet<SecurityControlTestCase> SecurityControlTestCases => Set<SecurityControlTestCase>();
    public DbSet<NessusTestCase> NessusTestCases => Set<NessusTestCase>();
    public DbSet<NISTControl> NISTControls => Set<NISTControl>();
    public DbSet<CCI> CCIs => Set<CCI>();
    public DbSet<NISTControlAssessment> NISTControlAssessments => Set<NISTControlAssessment>();
    public DbSet<ControlDocumentationRequirement> ControlDocumentationRequirements => Set<ControlDocumentationRequirement>();
    public DbSet<ControlDocumentationInstance> ControlDocumentationInstances => Set<ControlDocumentationInstance>();
    public DbSet<ADConfiguration> ADConfigurations => Set<ADConfiguration>();

    // Vulnerability Management Import
    public DbSet<VulnImportSession> VulnImportSessions => Set<VulnImportSession>();
    public DbSet<VulnImportHost> VulnImportHosts => Set<VulnImportHost>();
    public DbSet<VulnImportStigResult> VulnImportStigResults => Set<VulnImportStigResult>();
    public DbSet<VulnImportNessusVuln> VulnImportNessusVulns => Set<VulnImportNessusVuln>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // SystemInfo configuration
        modelBuilder.Entity<SystemInfo>(entity =>
        {
            entity.HasOne(s => s.ISSM)
                .WithMany(u => u.ManagedSystems)
                .HasForeignKey(s => s.ISSMId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // POAM configuration
        modelBuilder.Entity<POAM>(entity =>
        {
            entity.HasIndex(e => e.ItemIdentifier);

            entity.HasOne(p => p.System)
                .WithMany(s => s.POAMs)
                .HasForeignKey(p => p.SystemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.POC)
                .WithMany(u => u.POAMsAsPOC)
                .HasForeignKey(p => p.POCId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Milestone configuration
        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.HasOne(m => m.POAM)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.POAMId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.AssignedTo)
                .WithMany(u => u.AssignedMilestones)
                .HasForeignKey(m => m.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserAssignment configuration
        modelBuilder.Entity<UserAssignment>(entity =>
        {
            entity.HasOne(ua => ua.POAM)
                .WithMany(p => p.UserAssignments)
                .HasForeignKey(ua => ua.POAMId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ua => ua.User)
                .WithMany(u => u.Assignments)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ua => ua.AssignedBy)
                .WithMany()
                .HasForeignKey(ua => ua.AssignedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Prevent duplicate assignments
            entity.HasIndex(ua => new { ua.POAMId, ua.UserId, ua.AssignmentType }).IsUnique();
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });

            entity.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SecurityTestPlan configuration
        modelBuilder.Entity<SecurityTestPlan>(entity =>
        {
            entity.HasIndex(e => e.STPIdentifier).IsUnique();

            entity.HasOne(s => s.System)
                .WithMany()
                .HasForeignKey(s => s.SystemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.POAM)
                .WithMany()
                .HasForeignKey(s => s.POAMId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.LeadAssessor)
                .WithMany()
                .HasForeignKey(s => s.LeadAssessorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.ISSM)
                .WithMany()
                .HasForeignKey(s => s.ISSMId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // STPTestCase configuration (STIG)
        modelBuilder.Entity<STPTestCase>(entity =>
        {
            entity.HasIndex(e => new { e.SecurityTestPlanId, e.VulnId });

            entity.HasOne(t => t.SecurityTestPlan)
                .WithMany(s => s.TestCases)
                .HasForeignKey(t => t.SecurityTestPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Tester)
                .WithMany()
                .HasForeignKey(t => t.TesterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SecurityControlTestCase configuration (NIST 800-53)
        modelBuilder.Entity<SecurityControlTestCase>(entity =>
        {
            entity.HasIndex(e => new { e.SecurityTestPlanId, e.ControlId });

            entity.HasOne(t => t.SecurityTestPlan)
                .WithMany(s => s.ControlTestCases)
                .HasForeignKey(t => t.SecurityTestPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NessusTestCase configuration (Vulnerability)
        modelBuilder.Entity<NessusTestCase>(entity =>
        {
            entity.HasIndex(e => new { e.SecurityTestPlanId, e.PluginId });

            entity.HasOne(t => t.SecurityTestPlan)
                .WithMany(s => s.NessusTestCases)
                .HasForeignKey(t => t.SecurityTestPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Tester)
                .WithMany()
                .HasForeignKey(t => t.TesterId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // NISTControl configuration (reference data)
        modelBuilder.Entity<NISTControl>(entity =>
        {
            entity.HasIndex(e => e.ControlId).IsUnique();
            entity.HasIndex(e => e.Family);
        });

        // CCI configuration
        modelBuilder.Entity<CCI>(entity =>
        {
            entity.HasIndex(e => e.CCINumber);

            entity.HasOne(c => c.NISTControl)
                .WithMany(n => n.CCIs)
                .HasForeignKey(c => c.NISTControlId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NISTControlAssessment configuration (per-system compliance history)
        modelBuilder.Entity<NISTControlAssessment>(entity =>
        {
            // Index for fast lookup (not unique - allows multiple assessments over time)
            entity.HasIndex(e => new { e.NISTControlId, e.SystemId });
            entity.HasIndex(e => e.AssessedDate);

            entity.HasOne(a => a.NISTControl)
                .WithMany(n => n.Assessments)
                .HasForeignKey(a => a.NISTControlId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.System)
                .WithMany()
                .HasForeignKey(a => a.SystemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.AssessedBy)
                .WithMany()
                .HasForeignKey(a => a.AssessedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Soft link to POAM - no cascade
            entity.HasOne(a => a.POAM)
                .WithMany()
                .HasForeignKey(a => a.POAMId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ControlDocumentationRequirement configuration (reference data from CSV)
        modelBuilder.Entity<ControlDocumentationRequirement>(entity =>
        {
            // Index for fast lookup by control
            entity.HasIndex(e => e.NISTControlId);

            // Unique constraint: one doc type per control requirement
            entity.HasIndex(e => new { e.NISTControlId, e.DocType }).IsUnique();

            entity.HasOne(d => d.NISTControl)
                .WithMany()
                .HasForeignKey(d => d.NISTControlId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ControlDocumentationInstance configuration (per-system tracking)
        modelBuilder.Entity<ControlDocumentationInstance>(entity =>
        {
            // Index for fast lookup
            entity.HasIndex(e => new { e.RequirementId, e.SystemId });
            entity.HasIndex(e => e.SystemId);

            // Unique constraint: one instance per requirement per system
            entity.HasIndex(e => new { e.RequirementId, e.SystemId }).IsUnique();

            entity.HasOne(d => d.Requirement)
                .WithMany(r => r.Instances)
                .HasForeignKey(d => d.RequirementId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.System)
                .WithMany()
                .HasForeignKey(d => d.SystemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Owner)
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.ReviewedBy)
                .WithMany()
                .HasForeignKey(d => d.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ADConfiguration (singleton-like settings)
        modelBuilder.Entity<ADConfiguration>(entity =>
        {
            entity.HasOne(a => a.ModifiedBy)
                .WithMany()
                .HasForeignKey(a => a.ModifiedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // VulnImportSession configuration
        modelBuilder.Entity<VulnImportSession>(entity =>
        {
            entity.HasIndex(e => e.ImportDate);
        });

        // VulnImportHost configuration
        modelBuilder.Entity<VulnImportHost>(entity =>
        {
            entity.HasIndex(e => e.VulnImportSessionId);

            entity.HasOne(h => h.Session)
                .WithMany(s => s.Hosts)
                .HasForeignKey(h => h.VulnImportSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VulnImportStigResult configuration
        modelBuilder.Entity<VulnImportStigResult>(entity =>
        {
            entity.HasIndex(e => e.VulnImportHostId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(r => r.Host)
                .WithMany(h => h.StigResults)
                .HasForeignKey(r => r.VulnImportHostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // VulnImportNessusVuln configuration
        modelBuilder.Entity<VulnImportNessusVuln>(entity =>
        {
            entity.HasIndex(e => e.VulnImportHostId);
            entity.HasIndex(e => e.Severity);

            entity.HasOne(v => v.Host)
                .WithMany(h => h.NessusVulns)
                .HasForeignKey(v => v.VulnImportHostId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user)
            {
                user.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    user.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is SystemInfo system)
            {
                system.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    system.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is POAM poam)
            {
                poam.ModifiedDate = DateTime.UtcNow;
                poam.LastUpdateDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    poam.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is Milestone milestone)
            {
                milestone.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    milestone.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is SecurityTestPlan stp)
            {
                stp.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    stp.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is STPTestCase testCase)
            {
                testCase.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    testCase.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is SecurityControlTestCase controlTestCase)
            {
                controlTestCase.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    controlTestCase.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is NessusTestCase nessusTestCase)
            {
                nessusTestCase.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    nessusTestCase.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is NISTControl nistControl)
            {
                nistControl.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    nistControl.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is CCI cci)
            {
                cci.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    cci.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is NISTControlAssessment assessment)
            {
                assessment.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    assessment.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is ControlDocumentationRequirement docReq)
            {
                docReq.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    docReq.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is ControlDocumentationInstance docInst)
            {
                docInst.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    docInst.CreatedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is ADConfiguration adConfig)
            {
                adConfig.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                    adConfig.CreatedDate = DateTime.UtcNow;
            }
        }
    }
}
