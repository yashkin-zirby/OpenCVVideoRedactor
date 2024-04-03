using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OpenCVVideoRedactor.Model.Database
{
    public partial class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {
        }

        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Operation> Operations { get; set; } = null!;
        public virtual DbSet<Parameter> Parameters { get; set; } = null!;
        public virtual DbSet<Project> Projects { get; set; } = null!;
        public virtual DbSet<Resource> Resources { get; set; } = null!;
        public virtual DbSet<Variable> Variables { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Filename=database.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Operation>(entity =>
            {
                entity.ToTable("OPERATION");

                entity.Property(e => e.Id)
                    .ValueGeneratedNever()
                    .HasColumnName("ID");

                entity.Property(e => e.Name)
                    .HasColumnName("NAME");

                entity.Property(e => e.Next).HasColumnName("NEXT");

                entity.Property(e => e.Source).HasColumnName("SOURCE");

                entity.HasOne(d => d.NextNavigation)
                    .WithMany(p => p.InverseNextNavigation)
                    .HasForeignKey(d => d.Next);

                entity.HasOne(d => d.SourceNavigation)
                    .WithMany(p => p.Operations)
                    .HasForeignKey(d => d.Source)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Parameter>(entity =>
            {
                entity.HasKey(e => new { e.Name, e.Operation });

                entity.ToTable("PARAMETER");

                entity.Property(e => e.Name).HasColumnName("NAME");

                entity.Property(e => e.Operation).HasColumnName("OPERATION");

                entity.Property(e => e.Type).HasColumnName("TYPE");

                entity.Property(e => e.Value)
                    .HasColumnName("VALUE")
                    .HasDefaultValueSql("''");

                entity.HasOne(d => d.OperationNavigation)
                    .WithMany(p => p.Parameters)
                    .HasForeignKey(d => d.Operation)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("PROJECT");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Background).HasColumnName("BACKGROUND");

                entity.Property(e => e.DataFolder).HasColumnName("DATA_FOLDER");

                entity.Property(e => e.Title).HasColumnName("TITLE");

                entity.Property(e => e.VideoFps).HasColumnName("VIDEO_FPS");

                entity.Property(e => e.VideoHeight).HasColumnName("VIDEO_HEIGHT");

                entity.Property(e => e.VideoWidth).HasColumnName("VIDEO_WIDTH");
            });

            modelBuilder.Entity<Resource>(entity =>
            {
                entity.ToTable("RESOURCE");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Duration).HasColumnName("DURATION");

                entity.Property(e => e.Name).HasColumnName("NAME");

                entity.Property(e => e.PossitionX).HasColumnName("POSSITION_X");

                entity.Property(e => e.PossitionY).HasColumnName("POSSITION_Y");

                entity.Property(e => e.ProjectId).HasColumnName("PROJECT_ID");

                entity.Property(e => e.StartTime).HasColumnName("START_TIME");

                entity.Property(e => e.Type).HasColumnName("TYPE");

                entity.Property(e => e.Layer).HasColumnName("LAYER");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.Resources)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Variable>(entity =>
            {
                entity.HasKey(e => new { e.Name, e.Resource });

                entity.ToTable("VARIABLE");

                entity.Property(e => e.Name).HasColumnName("NAME");

                entity.Property(e => e.Resource).HasColumnName("RESOURCE");

                entity.Property(e => e.Value)
                    .HasColumnName("VALUE");

                entity.HasOne(d => d.ResourceNavigation)
                    .WithMany(p => p.Variables)
                    .HasForeignKey(d => d.Resource)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
