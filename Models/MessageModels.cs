using System;

namespace MyApp.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: conversations
// ─────────────────────────────────────────────────────────────────────────────
public class Conversation
{
    public int      Id            { get; set; }
    public int      StudentId     { get; set; }
    public int      TutorId       { get; set; }

    /// <summary>Preview tin nhắn cuối</summary>
    public string   LastMessage   { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Student          { get; set; }
    public User? Tutor            { get; set; }

    /// <summary>Tính khi query, không lưu DB</summary>
    public int UnreadCount        { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Bảng: messages
// ─────────────────────────────────────────────────────────────────────────────
public class Message
{
    public int      Id             { get; set; }
    public int      ConversationId { get; set; }
    public int      SenderId       { get; set; }

    /// <summary>Nội dung text, rỗng nếu là file</summary>
    public string   Content        { get; set; } = string.Empty;

    /// <summary>Đường dẫn tuyệt đối trên máy server</summary>
    public string?  FilePath       { get; set; }

    /// <summary>Tên file gốc để hiển thị cho người dùng</summary>
    public string?  FileName       { get; set; }

    /// <summary>"text" | "image" | "file"</summary>
    public string   MessageType    { get; set; } = "text";

    public bool     IsRead         { get; set; } = false;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? Sender            { get; set; }
}