﻿using System;
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

    public virtual DbSet<ConcurrentExecution> ConcurrentExecutions { get; set; }

    public virtual DbSet<ConcurrentQueue> ConcurrentQueues { get; set; }

    public virtual DbSet<GlobalConfig> GlobalConfigs { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<HistoryLastLog> HistoryLastLogs { get; set; }

    public virtual DbSet<JobAudit> JobAudits { get; set; }

    public virtual DbSet<JobCounter> JobCounters { get; set; }

    public virtual DbSet<JobDurationStatistic> JobDurationStatistics { get; set; }

    public virtual DbSet<JobEffectedRowsStatistic> JobEffectedRowsStatistics { get; set; }

    public virtual DbSet<JobInstanceLog> JobInstanceLogs { get; set; }

    public virtual DbSet<JobProperty> JobProperties { get; set; }

    public virtual DbSet<MonitorAction> MonitorActions { get; set; }

    public virtual DbSet<MonitorAlert> MonitorAlerts { get; set; }

    public virtual DbSet<MonitorCounter> MonitorCounters { get; set; }

    public virtual DbSet<MonitorHook> MonitorHooks { get; set; }

    public virtual DbSet<MonitorMute> MonitorMutes { get; set; }

    public virtual DbSet<SecurityAudit> SecurityAudits { get; set; }

    public virtual DbSet<Trace> Traces { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcurrentExecution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ConcurentExecution");
        });

        modelBuilder.Entity<ConcurrentQueue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ConcurentQueue");
        });

        modelBuilder.Entity<MonitorAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Monitor");

            entity.Property(e => e.Active).HasDefaultValue(true);

            entity.HasMany(d => d.Groups).WithMany(p => p.Monitors)
                .UsingEntity<Dictionary<string, object>>(
                    "MonitorActionsGroup",
                    r => r.HasOne<Group>().WithMany()
                        .HasForeignKey("GroupId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MonitorActionsGroups_Groups"),
                    l => l.HasOne<MonitorAction>().WithMany()
                        .HasForeignKey("MonitorId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MonitorActionsGroups_MonitorActions"),
                    j =>
                    {
                        j.HasKey("MonitorId", "GroupId");
                        j.ToTable("MonitorActionsGroups");
                    });
        });

        modelBuilder.Entity<MonitorCounter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MonitorCounter");
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
