using Microsoft.EntityFrameworkCore;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;

namespace Reembolso.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<ManagerCostCenterScope> ManagerCostCenterScopes => Set<ManagerCostCenterScope>();
    public DbSet<ReimbursementCategory> ReimbursementCategories => Set<ReimbursementCategory>();
    public DbSet<ReimbursementRequest> ReimbursementRequests => Set<ReimbursementRequest>();
    public DbSet<ReimbursementAttachment> ReimbursementAttachments => Set<ReimbursementAttachment>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureCostCenters(modelBuilder);
        ConfigureCategories(modelBuilder);
        ConfigureReimbursements(modelBuilder);
        ConfigureAttachments(modelBuilder);
        ConfigureWorkflowActions(modelBuilder);
        ConfigureAuditEntries(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureRefreshSessions(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();
        entity.ToTable("users");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
        entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        entity.HasIndex(x => x.Email).IsUnique();
        entity.HasOne(x => x.PrimaryCostCenter)
            .WithMany()
            .HasForeignKey(x => x.PrimaryCostCenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCostCenters(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CostCenter>();
        entity.ToTable("cost_centers");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();

        var scope = modelBuilder.Entity<ManagerCostCenterScope>();
        scope.ToTable("manager_cost_center_scopes");
        scope.HasKey(x => new { x.ManagerId, x.CostCenterId });
        scope.HasOne(x => x.Manager)
            .WithMany(x => x.ManagedCostCenters)
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Cascade);
        scope.HasOne(x => x.CostCenter)
            .WithMany()
            .HasForeignKey(x => x.CostCenterId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureCategories(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ReimbursementCategory>();
        entity.ToTable("reimbursement_categories");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
        entity.HasIndex(x => x.Name).IsUnique();
        entity.Property(x => x.MaxAmount).HasPrecision(18, 2);
        entity.Property(x => x.ReceiptRequiredAboveAmount).HasPrecision(18, 2);
    }

    private static void ConfigureReimbursements(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ReimbursementRequest>();
        entity.ToTable("reimbursement_requests");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.RequestNumber).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
        entity.Property(x => x.Amount).HasPrecision(18, 2);
        entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        entity.Property(x => x.RejectionReason).HasMaxLength(500);
        entity.Property(x => x.RowVersion).IsConcurrencyToken();
        entity.HasIndex(x => x.RequestNumber).IsUnique();
        entity.HasIndex(x => new { x.CreatedByUserId, x.CreatedAt });
        entity.HasIndex(x => new { x.CostCenterId, x.Status, x.SubmittedAt });
        entity.HasIndex(x => new { x.CategoryId, x.ExpenseDate });
        entity.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CostCenter)
            .WithMany()
            .HasForeignKey(x => x.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAttachments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ReimbursementAttachment>();
        entity.ToTable("reimbursement_attachments");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        entity.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();
        entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        entity.HasOne(x => x.Request)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureWorkflowActions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<WorkflowAction>();
        entity.ToTable("workflow_actions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        entity.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(40);
        entity.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        entity.Property(x => x.Comment).HasMaxLength(500);
        entity.HasIndex(x => new { x.RequestId, x.OccurredAt });
        entity.HasOne(x => x.Request)
            .WithMany(x => x.WorkflowActions)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAuditEntries(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AuditEntry>();
        entity.ToTable("audit_entries");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.EntityId).HasMaxLength(100);
        entity.Property(x => x.IpAddress).HasMaxLength(80);
        entity.Property(x => x.UserAgent).HasMaxLength(250);
        entity.Property(x => x.Severity).HasConversion<string>().HasMaxLength(30).IsRequired();
        entity.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PaymentRecord>();
        entity.ToTable("payment_records");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.AmountPaid).HasPrecision(18, 2);
        entity.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(40).IsRequired();
        entity.Property(x => x.PaymentReference).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Notes).HasMaxLength(500);
        entity.HasIndex(x => x.RequestId).IsUnique();
        entity.HasOne(x => x.Request)
            .WithOne(x => x.PaymentRecord)
            .HasForeignKey<PaymentRecord>(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureRefreshSessions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RefreshSession>();
        entity.ToTable("refresh_sessions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        entity.Property(x => x.CreatedByIp).HasMaxLength(80);
        entity.Property(x => x.UserAgent).HasMaxLength(250);
        entity.HasIndex(x => x.TokenHash).IsUnique();
        entity.HasIndex(x => new { x.UserId, x.FamilyId, x.RevokedAt });
        entity.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

