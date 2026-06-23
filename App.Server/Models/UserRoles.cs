namespace App.Server.Models;

public static class UserRoles
{
    public const int Student = 1;
    public const int Teacher = 2;
    public const int Admin = 3;

    public const string StudentName = "student";
    public const string TeacherName = "teacher";
    public const string AdminName = "admin";

    public static bool IsStudent(int roleId) => roleId == Student;
    public static bool IsTeacher(int roleId) => roleId == Teacher;
    public static bool IsAdmin(int roleId) => roleId == Admin;

    public static bool IsKnownRole(int roleId) =>
        roleId == Student || roleId == Teacher || roleId == Admin;

    public static string GetName(int roleId) => roleId switch
    {
        Teacher => TeacherName,
        Admin => AdminName,
        _ => StudentName
    };
}
