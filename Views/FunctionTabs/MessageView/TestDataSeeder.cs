using System;
using System.Linq;
using System.Threading.Tasks;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp;

/// <summary>
/// Chỉ chạy khi DEBUG — tạo 2 user test + 1 conversation sẵn trong DB
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedAsync()
    {
        var userRepo    = new UserRepository(App.Db);
        var profileRepo = new ProfileRepository(App.Db);
        var msgRepo     = new MessageRepository(App.Db);

        // ── Tạo user student nếu chưa có ────────────────────────────────────
        int studentId;
        var student = await userRepo.GetByUsernameAsync("test_student");
        if (student == null)
        {
            studentId = await userRepo.CreateAsync(
                "test_student", HashPassword("123"), "student", "active");

            await profileRepo.SaveStudentAsync(new StudentProfile
            {
                UserId       = studentId,
                DisplayName  = "Học Sinh Test",
                Level        = "Lớp 12",
                Budget       = 200000,
                LearningMode = "online",
            });
        }
        else studentId = student.Id;

        // ── Tạo user tutor nếu chưa có ──────────────────────────────────────
        int tutorId;
        var tutor = await userRepo.GetByUsernameAsync("test_tutor");
        if (tutor == null)
        {
            tutorId = await userRepo.CreateAsync(
                "test_tutor", HashPassword("123"), "tutor", "active");

            await profileRepo.SaveTutorAsync(new TutorProfile
            {
                UserId       = tutorId,
                FullName     = "Nguyễn Văn A",
                Profession   = "Gia sư Toán",
                Degree       = "Đại học Bách Khoa",
                HourlyRate   = 250000,
                TeachingMode = "both",
                AdminStatus  = "approved",
            });
        }
        else tutorId = tutor.Id;

        // ── Tạo conversation + seed tin nhắn mẫu ────────────────────────────
        var convId = await msgRepo.GetOrCreateConversationAsync(studentId, tutorId);

        // Chỉ seed tin nhắn nếu conversation còn mới (chưa có tin nào)
        var existing = await App.Db.ScalarAsync<int>(
            "SELECT COUNT(1) FROM messages WHERE conversation_id = @Id",
            new { Id = convId });

        if (existing == 0)
        {
            var msgs = new[]
            {
                (tutorId,   "Xin chào em, anh có thể giúp gì cho em?"),
                (studentId, "Dạ em muốn học Toán lớp 12 ạ"),
                (tutorId,   "Được, anh dạy Toán 10-12. Em yếu phần nào?"),
                (studentId, "Em yếu tích phân và giới hạn ạ"),
                (tutorId,   "Ok để anh soạn lịch học cho em nhé!"),
            };

            foreach (var (senderId, content) in msgs)
                await msgRepo.SendTextAsync(convId, senderId, content);
        }

        // ── Gán session là student để test ──────────────────────────────────
        Session.CurrentUserId   = studentId;
        Session.CurrentUsername = "test_student";
        Session.CurrentRole     = "student";
    }

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}