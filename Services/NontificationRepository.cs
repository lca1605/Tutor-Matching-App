using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.Data.Repositories;

public class NotificationRepository
{
    private readonly Database _db;

    public NotificationRepository(Database db) => _db = db;

    // ─── Read ─────────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả thông báo của user, mới nhất trước</summary>
    public async Task<IEnumerable<Notification>> GetByUserAsync(int userId)
    {
        return await _db.QueryAsync<Notification>(@"
            SELECT id, user_id, type, title, body, is_read, created_at
            FROM notifications
            WHERE user_id = @UserId
            ORDER BY created_at DESC",
            new { UserId = userId });
    }

    /// <summary>Đếm thông báo chưa đọc</summary>
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.ScalarAsync<int>(@"
            SELECT COUNT(1) FROM notifications
            WHERE user_id = @UserId AND is_read = FALSE",
            new { UserId = userId });
    }

    // ─── Write ────────────────────────────────────────────────────────────────

    /// <summary>Tạo thông báo mới</summary>
    public async Task CreateAsync(int userId, string type, string title, string body)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO notifications (user_id, type, title, body)
            VALUES (@UserId, @Type, @Title, @Body)",
            new { UserId = userId, Type = type, Title = title, Body = body });
    }

    /// <summary>Đánh dấu 1 thông báo đã đọc</summary>
    public async Task MarkReadAsync(int notificationId)
    {
        await _db.ExecuteAsync(@"
            UPDATE notifications SET is_read = TRUE WHERE id = @Id",
            new { Id = notificationId });
    }

    /// <summary>Đánh dấu tất cả đã đọc</summary>
    public async Task MarkAllReadAsync(int userId)
    {
        await _db.ExecuteAsync(@"
            UPDATE notifications SET is_read = TRUE
            WHERE user_id = @UserId AND is_read = FALSE",
            new { UserId = userId });
    }

    // ─── Helpers tạo thông báo theo loại ─────────────────────────────────────

    /// <summary>Thông báo khi gia sư được duyệt/từ chối</summary>
    public Task NotifyApprovalAsync(int tutorUserId, bool approved)
        => CreateAsync(tutorUserId, "approval",
            approved ? "Hồ sơ được duyệt ✓" : "Hồ sơ bị từ chối",
            approved
                ? "Chúc mừng! Hồ sơ gia sư của bạn đã được admin phê duyệt."
                : "Hồ sơ gia sư của bạn chưa đạt yêu cầu. Vui lòng cập nhật và gửi lại.");

    /// <summary>Thông báo khi học sinh quan tâm đến gia sư</summary>
    public Task NotifyInterestAsync(int tutorUserId, string studentName)
        => CreateAsync(tutorUserId, "interest",
            "Có học sinh quan tâm",
            $"{studentName} vừa quan tâm đến hồ sơ của bạn.");

    /// <summary>Thông báo từ admin</summary>
    public Task NotifyAdminAsync(int userId, string title, string body)
        => CreateAsync(userId, "admin", title, body);

    public async Task StudentInterestAsync(int studentId, int tutorId, string studentName)
    {
        // Lưu vào bảng student_interests
        await _db.ExecuteAsync(@"
            INSERT INTO student_interests (student_id, tutor_id)
            VALUES (@StudentId, @TutorId)
            ON CONFLICT DO NOTHING",
            new { StudentId = studentId, TutorId = tutorId });

        // Gửi thông báo cho tutor
        await NotifyInterestAsync(tutorId, studentName);
    }
}