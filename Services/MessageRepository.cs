using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MyApp.Models;

namespace MyApp.Data.Repositories;

public class MessageRepository
{
    private readonly Database _db;

    /// <summary>
    /// Lưu file vào thư mục cố định — không bị mất khi dotnet clean
    /// Linux:   /home/user/.local/share/MyApp/uploads/
    /// Windows: C:\Users\user\AppData\Local\MyApp\uploads\
    /// </summary>
    public static readonly string FileStorageRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyApp", "uploads");

    public MessageRepository(Database db)
    {
        _db = db;
        Directory.CreateDirectory(FileStorageRoot);
    }

    // ─── Conversation ─────────────────────────────────────────────────────────

    /// <summary>Lấy hoặc tạo conversation giữa học sinh và gia sư</summary>
    public async Task<int> GetOrCreateConversationAsync(int studentId, int tutorId)
    {
        var existing = await _db.QueryFirstOrDefaultAsync<Conversation>(@"
            SELECT id FROM conversations
            WHERE student_id = @StudentId AND tutor_id = @TutorId",
            new { StudentId = studentId, TutorId = tutorId });

        if (existing != null) return existing.Id;

        return await _db.InsertAsync(@"
            INSERT INTO conversations (student_id, tutor_id, last_message)
            VALUES (@StudentId, @TutorId, '')
            RETURNING id",
            new { StudentId = studentId, TutorId = tutorId });
    }

    /// <summary>Lấy danh sách conversation của 1 user kèm unread count</summary>
    public async Task<IEnumerable<Conversation>> GetConversationsAsync(int userId)
    {
        return await _db.QueryAsync<Conversation>(@"
            SELECT
                c.id, c.student_id, c.tutor_id,
                c.last_message, c.last_message_at, c.created_at,
                (
                    SELECT COUNT(1) FROM messages m
                    WHERE m.conversation_id = c.id
                      AND m.sender_id != @UserId
                      AND m.is_read = FALSE
                ) AS unread_count
            FROM conversations c
            WHERE c.student_id = @UserId OR c.tutor_id = @UserId
            ORDER BY c.last_message_at DESC",
            new { UserId = userId });
    }

    // ─── Messages ─────────────────────────────────────────────────────────────

    /// <summary>Lấy tin nhắn trong conversation, mới nhất trước, phân trang</summary>
    public async Task<IEnumerable<Message>> GetMessagesAsync(
        int conversationId, int limit = 50, int offset = 0)
    {
        return await _db.QueryAsync<Message>(@"
            SELECT id, conversation_id, sender_id, content,
                   file_path, file_name, message_type, is_read, created_at
            FROM messages
            WHERE conversation_id = @ConversationId
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset",
            new { ConversationId = conversationId, Limit = limit, Offset = offset });
    }

    /// <summary>Gửi tin nhắn text</summary>
    public async Task<int> SendTextAsync(int conversationId, int senderId, string content)
    {
        var msgId = await _db.InsertAsync(@"
            INSERT INTO messages (conversation_id, sender_id, content, message_type)
            VALUES (@ConversationId, @SenderId, @Content, 'text')
            RETURNING id",
            new { ConversationId = conversationId, SenderId = senderId, Content = content });

        await UpdateLastMessageAsync(conversationId, content);
        return msgId;
    }

    /// <summary>
    /// Gửi file — copy vào FileStorageRoot rồi lưu đường dẫn vào DB
    /// </summary>
    public async Task<int> SendFileAsync(int conversationId, int senderId,
                                         string localFilePath)
    {
        var fileName  = Path.GetFileName(localFilePath);
        var extension = Path.GetExtension(localFilePath).ToLower();
        var savedName = $"{Guid.NewGuid()}{extension}";
        var destPath  = Path.Combine(FileStorageRoot, savedName);

        File.Copy(localFilePath, destPath, overwrite: true);

        var messageType = extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"
            ? "image" : "file";

        var preview = messageType == "image" ? "📷 Hình ảnh" : $"📎 {fileName}";

        var msgId = await _db.InsertAsync(@"
            INSERT INTO messages
                (conversation_id, sender_id, content, file_path, file_name, message_type)
            VALUES
                (@ConversationId, @SenderId, @Content, @FilePath, @FileName, @MessageType)
            RETURNING id",
            new
            {
                ConversationId = conversationId,
                SenderId       = senderId,
                Content        = preview,
                FilePath       = destPath,
                FileName       = fileName,
                MessageType    = messageType,
            });

        await UpdateLastMessageAsync(conversationId, preview);
        return msgId;
    }

    /// <summary>Đánh dấu đã đọc tất cả tin nhắn từ người kia</summary>
    public async Task MarkAsReadAsync(int conversationId, int readerId)
    {
        await _db.ExecuteAsync(@"
            UPDATE messages
            SET is_read = TRUE
            WHERE conversation_id = @ConversationId
              AND sender_id != @ReaderId
              AND is_read = FALSE",
            new { ConversationId = conversationId, ReaderId = readerId });
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private async Task UpdateLastMessageAsync(int conversationId, string preview)
    {
        await _db.ExecuteAsync(@"
            UPDATE conversations
            SET last_message    = @LastMessage,
                last_message_at = NOW()
            WHERE id = @Id",
            new { LastMessage = preview, Id = conversationId });
    }
}