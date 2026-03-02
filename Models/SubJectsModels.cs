using System;
namespace MyApp.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: subjects
// ─────────────────────────────────────────────────────────────────────────────
public class Subject
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;

    /// <summary>"Tự nhiên" | "Xã hội" | "Ngoại ngữ" | "Kỹ năng" | "Khác"</summary>
    public string Category { get; set; } = string.Empty;

    public bool   IsActive { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: tutor_subjects  (many-to-many: tutor_profiles <-> subjects)
// ─────────────────────────────────────────────────────────────────────────────
public class TutorSubject
{
    public int TutorProfileId { get; set; }
    public int SubjectId      { get; set; }

    // Navigation
    public TutorProfile? TutorProfile { get; set; }
    public Subject?      Subject      { get; set; }
}