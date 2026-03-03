using System;
using System.Threading.Tasks;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp;

public static class TestDataSeeder
{
    public static async Task SeedAsync()
    {
        var userRepo    = new UserRepository(App.Db);
        var profileRepo = new ProfileRepository(App.Db);
        var msgRepo     = new MessageRepository(App.Db);
        var notiRepo    = new NotificationRepository(App.Db);

        // ── Student ──────────────────────────────────────────────────────────
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

        // ── Tutor ────────────────────────────────────────────────────────────
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
        // ── Tutor 2 ──────────────────────────────────────────────────────────────
        int tutor2Id;
        var tutor2 = await userRepo.GetByUsernameAsync("test_tutor2");
        if (tutor2 == null)
        {
            tutor2Id = await userRepo.CreateAsync(
                "test_tutor2", HashPassword("123"), "tutor", "active");

            await profileRepo.SaveTutorAsync(new TutorProfile
            {
                UserId       = tutor2Id,
                FullName     = "Trần Thị B",
                Profession   = "Gia sư Tiếng Anh",
                Degree       = "Đại học Ngoại ngữ Hà Nội",
                Description  = "Giáo viên Tiếng Anh 5 năm kinh nghiệm, chuyên luyện IELTS và giao tiếp.",
                HourlyRate   = 200000,
                TeachingMode = "online",
                AdminStatus  = "approved",
            });

            var profile2 = await profileRepo.GetTutorByUserIdAsync(tutor2Id);
            if (profile2 != null)
            {
                var engSubject = await App.Db.QueryFirstOrDefaultAsync<Subject>(
                    "SELECT id FROM subjects WHERE name = 'Tiếng Anh'");
                if (engSubject != null)
                    await new SubjectRepository(App.Db)
                        .SaveTutorSubjectsAsync(profile2.Id, new[] { engSubject.Id });
            }
        }
        else tutor2Id = tutor2.Id;

        // ── Tutor 3 ──────────────────────────────────────────────────────────────
        int tutor3Id;
        var tutor3 = await userRepo.GetByUsernameAsync("test_tutor3");
        if (tutor3 == null)
        {
            tutor3Id = await userRepo.CreateAsync(
                "test_tutor3", HashPassword("123"), "tutor", "active");

            await profileRepo.SaveTutorAsync(new TutorProfile
            {
                UserId       = tutor3Id,
                FullName     = "Lê Văn C",
                Profession   = "Gia sư Vật lý & Hóa học",
                Degree       = "Đại học Khoa học Tự nhiên TP.HCM",
                Description  = "Cựu sinh viên xuất sắc, dạy kèm Lý Hóa cho học sinh THPT luyện thi đại học.",
                HourlyRate   = 180000,
                TeachingMode = "both",
                AdminStatus  = "approved",
            });

            var profile3 = await profileRepo.GetTutorByUserIdAsync(tutor3Id);
            if (profile3 != null)
            {
                var subjects = await App.Db.QueryAsync<Subject>(
                    "SELECT id FROM subjects WHERE name IN ('Vật lý', 'Hóa học')");
                await new SubjectRepository(App.Db)
                    .SaveTutorSubjectsAsync(profile3.Id, subjects.Select(s => s.Id));
            }
        }
        else tutor3Id = tutor3.Id;

        // ── Conversation + tin nhắn mẫu ──────────────────────────────────────
        var convId = await msgRepo.GetOrCreateConversationAsync(studentId, tutorId);

        var msgCount = await App.Db.ScalarAsync<int>(
            "SELECT COUNT(1) FROM messages WHERE conversation_id = @Id",
            new { Id = convId });

        if (msgCount == 0)
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

        // ── Thông báo mẫu ────────────────────────────────────────────────────
        var notiCount = await App.Db.ScalarAsync<int>(
            "SELECT COUNT(1) FROM notifications WHERE user_id = @Id",
            new { Id = studentId });

        if (notiCount == 0)
        {
            // Thông báo cho student
            await notiRepo.NotifyAdminAsync(studentId,
                "Chào mừng đến với ứng dụng! 🎉",
                "Bạn có thể tìm gia sư phù hợp và bắt đầu học ngay hôm nay.");

            await notiRepo.NotifyInterestAsync(studentId,
                "Nguyễn Văn A");

            // Thông báo cho tutor
            await notiRepo.NotifyAdminAsync(tutorId,
                "Chào mừng gia sư mới! 🎉",
                "Hồ sơ của bạn đang chờ admin xét duyệt. Chúng tôi sẽ thông báo sớm nhất.");

            await notiRepo.NotifyApprovalAsync(tutorId, approved: true);
        }

        await App.Db.ExecuteAsync(@"
            INSERT INTO student_interests (student_id, tutor_id)
            VALUES (@StudentId, @TutorId)
            ON CONFLICT DO NOTHING",
            new { StudentId = studentId, TutorId = tutorId });

        // ── Gán session là student ────────────────────────────────────────────
        var typeUser = 0;
        if (typeUser == 0)
        {
            Session.CurrentUserId   = studentId;
            Session.CurrentUsername = "test_student";
            Session.CurrentRole     = "student";
        } 
        else
        {
            Session.CurrentUserId   = tutorId;
            Session.CurrentUsername = "test_tutor";
            Session.CurrentRole     = "tutor";
        }
    }

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}