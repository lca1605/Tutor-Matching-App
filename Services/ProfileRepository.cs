using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.Data.Repositories;

public class ProfileRepository
{
    private readonly Database _db;

    public ProfileRepository(Database db) => _db = db;

    // ─── Student ──────────────────────────────────────────────────────────────

    /// <summary>Lưu profile học sinh, trả về Id</summary>
    public async Task<int> SaveStudentAsync(StudentProfile p)
    {
        return await _db.InsertAsync(@"
            INSERT INTO student_profiles
                (user_id, display_name, description, level, budget, requirement, learning_mode)
            VALUES
                (@UserId, @DisplayName, @Description, @Level, @Budget, @Requirement, @LearningMode)
            RETURNING id",
            new
            {
                p.UserId, p.DisplayName, p.Description,
                p.Level,  p.Budget,     p.Requirement, p.LearningMode,
            });
    }

    /// <summary>Lấy profile học sinh theo UserId</summary>
    public async Task<StudentProfile?> GetStudentByUserIdAsync(int userId)
    {
        return await _db.QueryFirstOrDefaultAsync<StudentProfile>(@"
            SELECT id, user_id, display_name, description, level,
                   budget, requirement, learning_mode, created_at
            FROM student_profiles
            WHERE user_id = @UserId",
            new { UserId = userId });
    }

    // ─── Tutor ────────────────────────────────────────────────────────────────

    /// <summary>Lưu profile gia sư, trả về Id</summary>
    public async Task<int> SaveTutorAsync(TutorProfile p)
    {
        return await _db.InsertAsync(@"
            INSERT INTO tutor_profiles
                (user_id, full_name, description, workplace, profession,
                 degree, cv_file_path, hourly_rate, teaching_mode, admin_status)
            VALUES
                (@UserId, @FullName, @Description, @Workplace, @Profession,
                 @Degree, @CvFilePath, @HourlyRate, @TeachingMode, @AdminStatus)
            RETURNING id",
            new
            {
                p.UserId,       p.FullName,    p.Description, p.Workplace,
                p.Profession,   p.Degree,      p.CvFilePath,  p.HourlyRate,
                p.TeachingMode, p.AdminStatus,
            });
    }

    /// <summary>Lưu danh sách môn gia sư dạy</summary>
    public async Task SaveTutorSubjectsAsync(int tutorProfileId, IEnumerable<int> subjectIds)
    {
        foreach (var subjectId in subjectIds)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO tutor_subjects (tutor_profile_id, subject_id)
                VALUES (@TutorProfileId, @SubjectId)
                ON CONFLICT DO NOTHING",
                new { TutorProfileId = tutorProfileId, SubjectId = subjectId });
        }
    }

    /// <summary>Lấy profile gia sư theo UserId</summary>
    public async Task<TutorProfile?> GetTutorByUserIdAsync(int userId)
    {
        return await _db.QueryFirstOrDefaultAsync<TutorProfile>(@"
            SELECT id, user_id, full_name, description, workplace, profession,
                   degree, cv_file_path, hourly_rate, teaching_mode, admin_status, created_at
            FROM tutor_profiles
            WHERE user_id = @UserId",
            new { UserId = userId });
    }
}