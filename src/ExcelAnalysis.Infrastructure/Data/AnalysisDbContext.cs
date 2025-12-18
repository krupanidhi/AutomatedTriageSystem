using ExcelAnalysis.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelAnalysis.Infrastructure.Data;

public class AnalysisDbContext : DbContext
{
    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options) : base(options)
    {
    }

    public DbSet<ExcelFileInfo> ExcelFiles { get; set; }
    public DbSet<ExcelRow> ExcelRows { get; set; }
    public DbSet<AnalysisResult> AnalysisResults { get; set; }
    public DbSet<RiskItem> RiskItems { get; set; }
    public DbSet<ProgressMetric> ProgressMetrics { get; set; }
    
    // Historical tracking tables
    public DbSet<HistoricalAnalysisSnapshot> HistoricalSnapshots { get; set; }
    public DbSet<HistoricalChallenge> HistoricalChallenges { get; set; }
    public DbSet<RemediationAttempt> RemediationAttempts { get; set; }
    public DbSet<OrganizationSnapshot> OrganizationSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ExcelFileInfo configuration
        modelBuilder.Entity<ExcelFileInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileHash).HasMaxLength(64);
            entity.HasIndex(e => e.FileHash);
            
            entity.HasMany(e => e.Rows)
                .WithOne(r => r.ExcelFileInfo)
                .HasForeignKey(r => r.ExcelFileInfoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.AnalysisResult)
                .WithOne(a => a.ExcelFileInfo)
                .HasForeignKey<AnalysisResult>(a => a.ExcelFileInfoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ExcelRow configuration
        modelBuilder.Entity<ExcelRow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataJson).IsRequired();
            entity.Property(e => e.SheetName).HasMaxLength(255);
            entity.HasIndex(e => e.SheetName);
        });

        // AnalysisResult configuration
        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasMany(e => e.RiskItems)
                .WithOne(r => r.AnalysisResult)
                .HasForeignKey(r => r.AnalysisResultId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.ProgressMetrics)
                .WithOne(p => p.AnalysisResult)
                .HasForeignKey(p => p.AnalysisResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RiskItem configuration
        modelBuilder.Entity<RiskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.Mitigation).IsRequired(false);
            entity.Property(e => e.Deliverable).HasMaxLength(500);
            entity.Property(e => e.Source).HasMaxLength(500);
            entity.Property(e => e.SheetName).HasMaxLength(255);
            entity.Property(e => e.FieldName).HasMaxLength(255);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.SheetName);
        });

        // ProgressMetric configuration
        modelBuilder.Entity<ProgressMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Deliverable).HasMaxLength(500);
        });

        // HistoricalAnalysisSnapshot configuration
        modelBuilder.Entity<HistoricalAnalysisSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnalysisType).HasMaxLength(50);
            entity.HasIndex(e => e.AnalyzedAt);
            entity.HasIndex(e => e.ExcelFileInfoId);
            
            entity.HasMany(e => e.Challenges)
                .WithOne(c => c.Snapshot)
                .HasForeignKey(c => c.HistoricalAnalysisSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(e => e.OrganizationSnapshots)
                .WithOne(o => o.Snapshot)
                .HasForeignKey(o => o.HistoricalAnalysisSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HistoricalChallenge configuration
        modelBuilder.Entity<HistoricalChallenge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChallengeName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.HasIndex(e => e.ChallengeName);
            entity.HasIndex(e => e.Status);
            
            entity.HasMany(e => e.RemediationAttempts)
                .WithOne(r => r.Challenge)
                .HasForeignKey(r => r.HistoricalChallengeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RemediationAttempt configuration
        modelBuilder.Entity<RemediationAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionTaken).IsRequired();
            entity.Property(e => e.ResponsibleParty).HasMaxLength(255);
            entity.Property(e => e.Outcome).HasMaxLength(50);
            entity.HasIndex(e => e.AttemptedOn);
            entity.HasIndex(e => e.Outcome);
        });

        // OrganizationSnapshot configuration
        modelBuilder.Entity<OrganizationSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrganizationName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RiskLevel).HasMaxLength(50);
            entity.HasIndex(e => e.OrganizationName);
        });
    }
}
