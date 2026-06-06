using System;
using System.Collections.Generic;
using FigurasQE_AuthenticationService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FigurasQE_AuthenticationService.Data;

public partial class FigurasqeContext : DbContext
{
    public FigurasqeContext()
    {
    }

    public FigurasqeContext(DbContextOptions<FigurasqeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<LevelResult> LevelResults { get; set; }

    public virtual DbSet<Prueba> Pruebas { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Tutor> Tutors { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=figurasqe;Username=postgres;Password=1234");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.IdAdmin).HasName("admins_pkey");

            entity.ToTable("admins");

            entity.HasIndex(e => e.Email, "admins_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "admins_username_key").IsUnique();

            entity.Property(e => e.IdAdmin).HasColumnName("id_admin");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registration_date");
            entity.Property(e => e.Username)
                .HasMaxLength(60)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => e.IdLevel).HasName("levels_pkey");

            entity.ToTable("levels");

            entity.Property(e => e.IdLevel).HasColumnName("id_level");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<LevelResult>(entity =>
        {
            entity.HasKey(e => e.IdResult).HasName("level_results_pkey");

            entity.ToTable("level_results");

            entity.Property(e => e.IdResult).HasColumnName("id_result");
            entity.Property(e => e.Attempts).HasColumnName("attempts");
            entity.Property(e => e.Completed).HasColumnName("completed");
            entity.Property(e => e.Fails).HasColumnName("fails");
            entity.Property(e => e.FinishingTime).HasColumnName("finishing_time");
            entity.Property(e => e.IdLevel).HasColumnName("id_level");
            entity.Property(e => e.IdSession).HasColumnName("id_session");

            entity.HasOne(d => d.IdLevelNavigation).WithMany(p => p.LevelResults)
                .HasForeignKey(d => d.IdLevel)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("level_results_id_level_fkey");

            entity.HasOne(d => d.IdSessionNavigation).WithMany(p => p.LevelResults)
                .HasForeignKey(d => d.IdSession)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("level_results_id_session_fkey");
        });

        modelBuilder.Entity<Prueba>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("prueba");

            entity.Property(e => e.Id).HasColumnName("id");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.IdSession).HasName("sessions_pkey");

            entity.ToTable("sessions");

            entity.Property(e => e.IdSession).HasColumnName("id_session");
            entity.Property(e => e.BeginningDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("beginning_date");
            entity.Property(e => e.Device)
                .HasMaxLength(50)
                .HasColumnName("device");
            entity.Property(e => e.EndDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("end_date");
            entity.Property(e => e.IdStudent).HasColumnName("id_student");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.IdStudent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sessions_id_student_fkey");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.IdStudent).HasName("students_pkey");

            entity.ToTable("students");

            entity.Property(e => e.IdStudent).HasColumnName("id_student");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Country)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("country");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");
            entity.Property(e => e.IdTutor).HasColumnName("id_tutor");
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("name");
            entity.Property(e => e.Neurodivergency)
                .HasMaxLength(50)
                .HasColumnName("neurodivergency");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registration_date");

            entity.HasOne(d => d.IdTutorNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.IdTutor)
                .HasConstraintName("students_id_tutor_fkey");
        });

        modelBuilder.Entity<Tutor>(entity =>
        {
            entity.HasKey(e => e.IdTutor).HasName("tutors_pkey");

            entity.ToTable("tutors");

            entity.HasIndex(e => e.Email, "tutors_email_key").IsUnique();

            entity.Property(e => e.IdTutor).HasColumnName("id_tutor");
            entity.Property(e => e.Age).HasColumnName("age");
            entity.Property(e => e.Country)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("country");
            entity.Property(e => e.Degree)
                .HasMaxLength(20)
                .HasColumnName("degree");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .HasColumnName("gender");
            entity.Property(e => e.Name)
                .HasMaxLength(120)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("registration_date");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
