using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

//Purpose: Central EF Core context declaring all DB sets (users, courses, notes, collaborations, permissions, submissions).
//Inputs/Outputs: Receives DbContextOptions; gives controllers/services LINQ access to SQLite tables.
//Depends on: Entity classes in App.Server/ORM, migrations in App.Server/Migrations.

namespace App.Server.ORM;

public partial class AppDbContext : DbContext
{
    // =============== Migrations instructions =============== 
    // ================== Create migrations ==================
    //
    // dotnet ef migrations add <name>
    //
    // =================== Update database ===================
    //
    // dotnet ef database update
    //
    // ============= If dotnet can't find ef =================
    // 
    // dotnet tool install --global dotnet-ef --version 8.*
    // 

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Collaboration> Collaborations { get; set; } = null!;
    public DbSet<CollaborationMember> CollaborationMembers { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<CourseNote> CoursesNotes { get; set; } = null!;
    public DbSet<CourseResource> CourseResources { get; set; } = null!;
    public DbSet<CourseWork> CourseWork { get; set; } = null!;
    public DbSet<CourseWorkResource> CourseWorkResources { get; set; } = null!;
    public DbSet<Grade> Grades { get; set; } = null!;
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<NoteFolder> NoteFolders { get; set; } = null!;
    public DbSet<NotePermission> NotePermissions { get; set; } = null!;
    public DbSet<SubmittedWork> SubmittedWork { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCourse> UsersCourses { get; set; } = null!;
    public DbSet<UserType> UserTypes { get; set; } = null!;
    public DbSet<VisibilityType> VisibilityTypes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Note>()
            .HasOne(n => n.Folder)
            .WithMany(f => f.Notes)
            .HasForeignKey(n => n.FolderId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<NoteFolder>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NoteFolder>()
            .HasOne(f => f.ParentFolder)
            .WithMany(f => f.ChildFolders)
            .HasForeignKey(f => f.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NotePermission>()
            .HasIndex(p => new { p.NoteId, p.UserId })
            .IsUnique();

        modelBuilder.Entity<NotePermission>()
            .HasOne(p => p.Note)
            .WithMany()
            .HasForeignKey(p => p.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotePermission>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<NotePermission>()
            .HasOne(p => p.InvitedByUser)
            .WithMany()
            .HasForeignKey(p => p.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
