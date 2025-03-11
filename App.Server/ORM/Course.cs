using System;
using System.Collections.Generic;

namespace App.Server.ORM;

public partial class Course
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<CourseWork> CourseWorks { get; set; } = new List<CourseWork>();

    public virtual ICollection<CoursesNote> CoursesNotes { get; set; } = new List<CoursesNote>();

    public virtual ICollection<UsersCourse> UsersCourses { get; set; } = new List<UsersCourse>();
}
