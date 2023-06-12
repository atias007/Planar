using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;

namespace Planar.Service.Data;

public partial class PlanarContext : DbContext
{
    public PlanarContext(DbContextOptions<PlanarContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ClusterNode> ClusterNodes { get; set; }

    public virtual DbSet<ConcurentQueue> ConcurentQueues { get; set; }

    public virtual DbSet<GlobalConfig> GlobalConfigs { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<JobAudit> JobAudits { get; set; }

    public virtual DbSet<JobCounter> JobCounters { get; set; }

    public virtual DbSet<JobDurationStatistic> JobDurationStatistics { get; set; }

    public virtual DbSet<JobEffectedRowsStatistic> JobEffectedRowsStatistics { get; set; }

    public virtual DbSet<JobInstanceLog> JobInstanceLogs { get; set; }

    public virtual DbSet<JobProperty> JobProperties { get; set; }

    public virtual DbSet<MonitorAction> MonitorActions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Trace> Traces { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasOne(d => d.Role).WithMany(p => p.Groups)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Groups_Roles");
        });

        modelBuilder.Entity<MonitorAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Monitor");

            entity.Property(e => e.Active).HasDefaultValueSql("((1))");

            entity.HasOne(d => d.Group).WithMany(p => p.MonitorActions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MonitorActions_Groups");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<Trace>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Log");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasMany(d => d.Groups).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UsersToGroup",
                    r => r.HasOne<Group>().WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UsersToGroups_Groups"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UsersToGroups_Users"),
                    j =>
                    {
                        j.HasKey("UserId", "GroupId");
                        j.ToTable("UsersToGroups");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
