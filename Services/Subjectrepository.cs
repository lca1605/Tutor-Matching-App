using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.Data.Repositories;

public class SubjectRepository
{
    private readonly Database _db;

    public SubjectRepository(Database db) => _db = db;

    /// <summary>Lấy tất cả môn đang active — dùng cho ComboBox trong TutorProfileView</summary>
    public async Task<IEnumerable<Subject>> GetAllActiveAsync()
    {
        return await _db.QueryAsync<Subject>(@"
            SELECT id, name, category, is_active
            FROM subjects
            WHERE is_active = true
            ORDER BY category, name");
    }

    /// <summary>Lấy các môn của 1 gia sư theo TutorProfileId</summary>
    public async Task<IEnumerable<Subject>> GetByTutorProfileIdAsync(int tutorProfileId)
    {
        return await _db.QueryAsync<Subject>(@"
            SELECT s.id, s.name, s.category, s.is_active
            FROM subjects s
            INNER JOIN tutor_subjects ts ON ts.subject_id = s.id
            WHERE ts.tutor_profile_id = @TutorProfileId
            ORDER BY s.name",
            new { TutorProfileId = tutorProfileId });
    }

    /// <summary>Lưu danh sách môn dạy của gia sư</summary>
    public async Task SaveTutorSubjectsAsync(int tutorProfileId, IEnumerable<int> subjectIds)
    {
        foreach (var subjectId in subjectIds)
            await _db.ExecuteAsync(@"
                INSERT INTO tutor_subjects (tutor_profile_id, subject_id)
                VALUES (@TutorProfileId, @SubjectId)
                ON CONFLICT DO NOTHING",
                new { TutorProfileId = tutorProfileId, SubjectId = subjectId });
    }
}