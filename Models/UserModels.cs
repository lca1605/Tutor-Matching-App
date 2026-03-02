using System;
using System.Collections.Generic;

namespace MyApp.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: users
// ─────────────────────────────────────────────────────────────────────────────
public class User
{
    public int      Id           { get; set; }
    public string   Username     { get; set; } = string.Empty;
    public string   PasswordHash { get; set; } = string.Empty;

    /// <summary>"student" | "tutor"</summary>
    public string   Role         { get; set; } = string.Empty;

    /// <summary>"active" | "pending" | "banned"</summary>
    public string   Status       { get; set; } = "active";

    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation — chỉ một trong hai sẽ có giá trị tuỳ Role
    public StudentProfile? StudentProfile { get; set; }
    public TutorProfile?   TutorProfile   { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: student_profiles
// ─────────────────────────────────────────────────────────────────────────────
public class StudentProfile
{
    public int      Id           { get; set; }
    public int      UserId       { get; set; }

    public string   DisplayName  { get; set; } = string.Empty;
    public string   Description  { get; set; } = string.Empty;

    /// <summary>VD: "Lớp 10", "Năm 2 Đại học"</summary>
    public string   Level        { get; set; } = string.Empty;

    /// <summary>Ngân sách mỗi giờ, đơn vị VND</summary>
    public decimal  Budget       { get; set; }

    public string   Requirement  { get; set; } = string.Empty;

    /// <summary>"online" | "offline" | "both"</summary>
    public string   LearningMode { get; set; } = "online";

    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: tutor_profiles
// ─────────────────────────────────────────────────────────────────────────────
public class TutorProfile
{
    public int      Id           { get; set; }
    public int      UserId       { get; set; }

    public string   FullName     { get; set; } = string.Empty;
    public string   Description  { get; set; } = string.Empty;
    public string   Workplace    { get; set; } = string.Empty;
    public string   Profession   { get; set; } = string.Empty;
    public string   Degree       { get; set; } = string.Empty;

    /// <summary>Đường dẫn file CV đã upload, null nếu chưa upload</summary>
    public string?  CvFilePath   { get; set; }

    /// <summary>Học phí mỗi giờ, đơn vị VND</summary>
    public decimal  HourlyRate   { get; set; }

    /// <summary>"online" | "offline" | "both"</summary>
    public string   TeachingMode { get; set; } = "online";

    /// <summary>"pending" | "approved" | "rejected"</summary>
    public string   AdminStatus  { get; set; } = "pending";

    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public User?          User     { get; set; }
    public List<Subject>  Subjects { get; set; } = new();
}