namespace MyApp;

/// <summary>
/// Lưu thông tin user đang đăng nhập trong phiên hiện tại.
/// Gán khi login thành công, xóa khi logout.
/// </summary>
public static class Session
{
    public static int    CurrentUserId   { get; set; }
    public static string CurrentUsername { get; set; } = string.Empty;

    /// <summary>"student" | "tutor"</summary>
    public static string CurrentRole     { get; set; } = string.Empty;

    public static bool IsLoggedIn => CurrentUserId > 0;

    public static void Clear()
    {
        CurrentUserId   = 0;
        CurrentUsername = string.Empty;
        CurrentRole     = string.Empty;
    }
}