using System;
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

        public virtual DbSet<ClusterServer> ClusterServers { get; set; }
        public virtual DbSet<GlobalParameter> GlobalParameters { get; set; }
        public virtual DbSet<Group> Groups { get; set; }
        public virtual DbSet<JobInstanceLog> JobInstanceLogs { get; set; }
        public virtual DbSet<MonitorAction> MonitorActions { get; set; }
        public virtual DbSet<Trace> Traces { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClusterServer>(entity =>
            {
                entity.HasKey(e => new { e.Server, e.Port, e.InstanceId })
                    .HasName("PK_ClusterServers_1");
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
