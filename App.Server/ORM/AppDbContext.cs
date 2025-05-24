using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using App.Server.Models;

namespace App.Server.ORM;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<CourseWork> CourseWorks { get; set; }

    public virtual DbSet<CoursesNote> CoursesNotes { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<SubmittedWork> SubmittedWorks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserType> UserTypes { get; set; }

    public virtual DbSet<UsersCourse> UsersCourses { get; set; }

    public virtual DbSet<UsersNote> UsersNotes { get; set; }

    public virtual DbSet<VisibilityType> VisibilityTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseWork>(entity =>
        {
            entity.ToTable("CourseWork");

            entity.HasOne(d => d.Course).WithMany(p => p.CourseWorks)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CoursesNote>(entity =>
        {
            entity.ToTable("Courses_Notes");

            entity.HasOne(d => d.Course).WithMany(p => p.CoursesNotes)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Note).WithMany(p => p.CoursesNotes)
                .HasForeignKey(d => d.NoteId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.Property(e => e.Grade1).HasColumnName("Grade");
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Notes_Guid").IsUnique();

            entity.Property(e => e.CreationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.ModifyDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.Notes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.VisibilityType).WithMany(p => p.Notes)
                .HasForeignKey(d => d.VisibilityTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SubmittedWork>(entity =>
        {
            entity.ToTable("SubmittedWork");

            entity.HasOne(d => d.Grade).WithMany(p => p.SubmittedWorks).HasForeignKey(d => d.GradeId);

            entity.HasOne(d => d.Note).WithMany(p => p.SubmittedWorks)
                .HasForeignKey(d => d.NoteId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.StudentId, "IX_Users_StudentId").IsUnique();

            entity.HasOne(d => d.UserType).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserType>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_UserTypes_Name").IsUnique();
        });

        modelBuilder.Entity<UsersCourse>(entity =>
        {
            entity.ToTable("Users_Courses");

            entity.HasOne(d => d.Course).WithMany(p => p.UsersCourses)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.UsersCourses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UsersNote>(entity =>
        {
            entity.ToTable("Users_Notes");

            entity.HasOne(d => d.Note).WithMany(p => p.UsersNotes)
                .HasForeignKey(d => d.NoteId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.UsersNotes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VisibilityType>(entity =>
        {
            entity.ToTable("VisibilityType");

            entity.HasIndex(e => e.Name, "IX_VisibilityType_Name").IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
