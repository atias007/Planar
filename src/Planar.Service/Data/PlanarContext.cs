﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Planar.Service.Model;

namespace Planar.Service.Data
{
    public partial class PlanarContext : DbContext
    {
        public PlanarContext()
        {
        }

        public PlanarContext(DbContextOptions<PlanarContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ClusterNode> ClusterNodes { get; set; } = null!;
        public virtual DbSet<ConcurentQueue> ConcurentQueues { get; set; } = null!;
        public virtual DbSet<GlobalConfig> GlobalConfigs { get; set; } = null!;
        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<JobDurationStatistic> JobDurationStatistics { get; set; } = null!;
        public virtual DbSet<JobEffectedRowsStatistic> JobEffectedRowsStatistics { get; set; } = null!;
        public virtual DbSet<JobInstanceLog> JobInstanceLogs { get; set; } = null!;
        public virtual DbSet<JobProperty> JobProperties { get; set; } = null!;
        public virtual DbSet<MonitorAction> MonitorActions { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<Trace> Traces { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClusterNode>(entity =>
            {
                entity.HasKey(e => new { e.Server, e.Port });
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Groups)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Groups_Roles");
            });

            modelBuilder.Entity<MonitorAction>(entity =>
            {
                entity.Property(e => e.Active).HasDefaultValueSql("((1))");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.MonitorActions)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MonitorActions_Groups");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasMany(d => d.Groups)
                    .WithMany(p => p.Users)
                    .UsingEntity<Dictionary<string, object>>(
                        "UsersToGroup",
                        l => l.HasOne<Group>().WithMany().HasForeignKey("GroupId").OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_UsersToGroups_Groups"),
                        r => r.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK_UsersToGroups_Users"),
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
}
