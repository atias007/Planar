using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;

#nullable disable

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

        public virtual DbSet<GlobalParameter> GlobalParameters { get; set; }
        public virtual DbSet<Group> Groups { get; set; }
        public virtual DbSet<JobInstanceLog> JobInstanceLogs { get; set; }
        public virtual DbSet<MonitorAction> MonitorActions { get; set; }
        public virtual DbSet<Trace> Traces { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UsersToGroup> UsersToGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<JobInstanceLog>(entity =>
            {
                entity.Property(e => e.InstanceId).IsUnicode(false);

                entity.Property(e => e.JobGroup).IsUnicode(false);

                entity.Property(e => e.JobId).IsUnicode(false);

                entity.Property(e => e.JobName).IsUnicode(false);

                entity.Property(e => e.StatusTitle).IsUnicode(false);

                entity.Property(e => e.TriggerGroup).IsUnicode(false);

                entity.Property(e => e.TriggerId).IsUnicode(false);

                entity.Property(e => e.TriggerName).IsUnicode(false);
            });

            modelBuilder.Entity<MonitorAction>(entity =>
            {
                entity.Property(e => e.Active).HasDefaultValueSql("((1))");

                entity.Property(e => e.Hook).IsUnicode(false);

                entity.Property(e => e.JobGroup).IsUnicode(false);

                entity.Property(e => e.JobId).IsUnicode(false);

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.MonitorActions)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MonitorActions_Groups");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Password).IsUnicode(false);

                entity.Property(e => e.Username).IsUnicode(false);
            });

            modelBuilder.Entity<UsersToGroup>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.GroupId });

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.UsersToGroups)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UsersToGroups_Groups");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UsersToGroups)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UsersToGroups_Users");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}