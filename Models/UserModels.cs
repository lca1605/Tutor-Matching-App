using System;
using System.Collections.Generic;

namespace MyApp.Models;

public class User
{
    public int      Id           { get; set; }
    public string   Username     { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;
    public string   Role         { get; set; } = string.Empty;
    public string   Status       { get; set; } = "active";

    /// <summary>Đường dẫn ảnh đại diện trên server, null nếu chưa có</summary>
    public string?  AvatarPath   { get; set; }

    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    public StudentProfile? StudentProfile { get; set; }
    public TutorProfile?   TutorProfile   { get; set; }
}

public class StudentProfile
{
    public int      Id           { get; set; }
    public int      UserId       { get; set; }
    public string   DisplayName  { get; set; } = string.Empty;
    public string   Description  { get; set; } = string.Empty;
    public string   Level        { get; set; } = string.Empty;
    public decimal  Budget       { get; set; }
    public string   Requirement  { get; set; } = string.Empty;
    public string   LearningMode { get; set; } = "online";
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public User?    User         { get; set; }
}

public class TutorProfile
{
    public int      Id           { get; set; }
    public int      UserId       { get; set; }
    public string   FullName     { get; set; } = string.Empty;
    public string   Description  { get; set; } = string.Empty;
    public string   Workplace    { get; set; } = string.Empty;
    public string   Profession   { get; set; } = string.Empty;
    public string   Degree       { get; set; } = string.Empty;
    public string?  CvFilePath   { get; set; }
    public decimal  HourlyRate   { get; set; }
    public string   TeachingMode { get; set; } = "online";
    public string   AdminStatus  { get; set; } = "pending";
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public User?          User     { get; set; }
    public List<Subject>  Subjects { get; set; } = new();
}