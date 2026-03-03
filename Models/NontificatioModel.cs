using System;

namespace MyApp.Models;

public class Notification
{
    public int      Id        { get; set; }
    public int      UserId    { get; set; }

    /// <summary>"interest" | "approval" | "admin"</summary>
    public string   Type      { get; set; } = string.Empty;

    public string   Title     { get; set; } = string.Empty;
    public string   Body      { get; set; } = string.Empty;
    public bool     IsRead    { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}