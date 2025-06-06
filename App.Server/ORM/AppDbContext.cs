using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<CourseWork> CourseWork { get; set; } = null!;
    public DbSet<Grade> Grades { get; set; } = null!;
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<NotePermission> NotePermissions { get; set; } = null!;
    public DbSet<SubmittedWork> SubmittedWork { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCourse> UsersCourses { get; set; } = null!;
    public DbSet<UserType> UserTypes { get; set; } = null!;
    public DbSet<VisibilityType> VisibilityTypes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
