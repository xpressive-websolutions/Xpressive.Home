using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;
using Xpressive.Home.Contracts.Services;
using Xpressive.Home.Services;
using Xpressive.Home.Services.Variables;

namespace Xpressive.Home.DatabaseModel
{
    public class XpressiveHomeContext : DbContext
    {
        public XpressiveHomeContext()
        {
        }

        public XpressiveHomeContext(DbContextOptions<XpressiveHomeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<DeviceDto> Device { get; set; }
        public virtual DbSet<Radio> Radio { get; set; }
        public virtual DbSet<Room> Room { get; set; }
        public virtual DbSet<RoomDevice> RoomDevice { get; set; }
        public virtual DbSet<RoomScript> RoomScript { get; set; }
        public virtual DbSet<RoomScriptGroup> RoomScriptGroup { get; set; }
        public virtual DbSet<ScheduledScript> ScheduledScript { get; set; }
        public virtual DbSet<Script> Script { get; set; }
        public virtual DbSet<TriggeredScript> TriggeredScript { get; set; }
        public virtual DbSet<PersistedVariable> Variable { get; set; }
        public virtual DbSet<WebHook> WebHook { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceDto>(entity =>
            {
                entity.HasKey(e => new { e.Gateway, e.Id });

                entity.Property(e => e.Gateway).HasMaxLength(64);

                entity.Property(e => e.Id).HasMaxLength(64);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(e => e.Properties).IsRequired();
            });

            modelBuilder.Entity<Radio>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasMaxLength(16)
                    .ValueGeneratedNever();

                entity.Property(e => e.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(128);
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Icon)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<RoomDevice>(entity =>
            {
                entity.HasKey(e => new { e.Gateway, e.Id });

                entity.Property(e => e.Gateway).HasMaxLength(16);

                entity.Property(e => e.Id).HasMaxLength(64);

                entity.HasOne(d => d.Room)
                    .WithMany(p => p.RoomDevice)
                    .HasForeignKey(d => d.RoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RoomDevice_Room");
            });

            modelBuilder.Entity<RoomScript>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<RoomScriptGroup>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Icon)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<ScheduledScript>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CronTab)
                    .IsRequired()
                    .HasMaxLength(32);
            });

            modelBuilder.Entity<Script>(entity =>
            {
                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasMaxLength(64)
                    .ValueGeneratedNever();

                entity.Property(e => e.JavaScript).IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(64);
            });

            modelBuilder.Entity<TriggeredScript>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Variable)
                    .IsRequired()
                    .HasMaxLength(255);
            });

            modelBuilder.Entity<PersistedVariable>(entity =>
            {
                entity.HasKey(e => e.Name);

                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .ValueGeneratedNever();

                entity.Property(e => e.DataType)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.Value).IsRequired();
            });

            modelBuilder.Entity<WebHook>(entity =>
            {
                entity.Property(e => e.Id)
                    .HasMaxLength(32)
                    .ValueGeneratedNever();

                entity.Property(e => e.DeviceId)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(e => e.GatewayName)
                    .IsRequired()
                    .HasMaxLength(16);
            });
        }
    }
}
