using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MyApp.Data.Repositories;

namespace MyApp.Views;

public partial class AccountView : UserControl
{
    private readonly UserRepository _userRepo;

    public AccountView()
    {
        _userRepo = new UserRepository(App.Db);
        InitializeComponent();
        LoadProfile();
    }

    // ─── Load thông tin ───────────────────────────────────────────────────────

    private async void LoadProfile()
    {
        if (Session.CurrentUserId == 0) return;

        var user = await _userRepo.GetByIdAsync(Session.CurrentUserId);
        if (user == null) return;

        UsernameText.Text = $"@{user.Username}";
        RoleText.Text     = user.Role == "tutor" ? "Gia sư" : "Học sinh";

        // Load tên hiển thị theo role
        if (user.Role == "student")
        {
            var profileRepo = new ProfileRepository(App.Db);
            var profile     = await profileRepo.GetStudentByUserIdAsync(user.Id);
            if (profile != null)
            {
                DisplayNameText.Text = profile.DisplayName;
                DisplayNameBox.Text  = profile.DisplayName;
            }
        }
        else
        {
            var profileRepo = new ProfileRepository(App.Db);
            var profile     = await profileRepo.GetTutorByUserIdAsync(user.Id);
            if (profile != null)
            {
                DisplayNameText.Text = profile.FullName;
                DisplayNameBox.Text  = profile.FullName;
            }
        }

        // Chữ cái đầu làm avatar fallback
        AvatarInitial.Text = !string.IsNullOrEmpty(DisplayNameText.Text)
            ? DisplayNameText.Text[0].ToString().ToUpper()
            : "?";

        // Load avatar nếu có
        if (!string.IsNullOrEmpty(user.AvatarPath) && File.Exists(user.AvatarPath))
        {
            AvatarImage.Source  = new Bitmap(user.AvatarPath);
            AvatarImage.IsVisible   = true;
            AvatarInitial.IsVisible = false;
        }
    }

    // ─── Đổi avatar ───────────────────────────────────────────────────────────

    private async void OnChangeAvatarClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title         = "Chọn ảnh đại diện",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Ảnh")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp" }
                    }
                }
            });

        if (files.Count == 0) return;

        var localPath = files[0].Path.LocalPath;
        var ext       = Path.GetExtension(localPath).ToLower();
        var savedName = $"avatar_{Session.CurrentUserId}{ext}";
        var destPath  = Path.Combine(MessageRepository.FileStorageRoot, savedName);

        File.Copy(localPath, destPath, overwrite: true);

        // Lưu đường dẫn vào DB
        await App.Db.ExecuteAsync(
            "UPDATE users SET avatar_path = @Path WHERE id = @Id",
            new { Path = destPath, Id = Session.CurrentUserId });

        // Cập nhật UI
        AvatarImage.Source      = new Bitmap(destPath);
        AvatarImage.IsVisible   = true;
        AvatarInitial.IsVisible = false;

        ShowResult(SaveResultText, "✓ Đã cập nhật ảnh đại diện", success: true);
    }

    // ─── Lưu tên hiển thị ────────────────────────────────────────────────────

    private async void OnSaveDisplayNameClick(object? sender, RoutedEventArgs e)
    {
        var name = DisplayNameBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            ShowResult(SaveResultText, "Tên hiển thị không được để trống.", success: false);
            return;
        }

        var user = await _userRepo.GetByIdAsync(Session.CurrentUserId);
        if (user == null) return;

        if (user.Role == "student")
        {
            await App.Db.ExecuteAsync(
                "UPDATE student_profiles SET display_name = @Name WHERE user_id = @UserId",
                new { Name = name, UserId = Session.CurrentUserId });
        }
        else
        {
            await App.Db.ExecuteAsync(
                "UPDATE tutor_profiles SET full_name = @Name WHERE user_id = @UserId",
                new { Name = name, UserId = Session.CurrentUserId });
        }

        DisplayNameText.Text = name;
        AvatarInitial.Text   = name[0].ToString().ToUpper();

        ShowResult(SaveResultText, "✓ Đã lưu tên hiển thị", success: true);
    }

    // ─── Đổi mật khẩu ────────────────────────────────────────────────────────

    private async void OnChangePasswordClick(object? sender, RoutedEventArgs e)
    {
        var oldPw  = OldPasswordBox.Text     ?? "";
        var newPw  = NewPasswordBox.Text     ?? "";
        var confirm= ConfirmPasswordBox.Text ?? "";

        if (string.IsNullOrEmpty(oldPw) || string.IsNullOrEmpty(newPw))
        {
            ShowResult(PasswordResultText, "Vui lòng điền đầy đủ thông tin.", success: false);
            return;
        }
        if (newPw.Length < 6)
        {
            ShowResult(PasswordResultText, "Mật khẩu mới phải có ít nhất 6 ký tự.", success: false);
            return;
        }
        if (newPw != confirm)
        {
            ShowResult(PasswordResultText, "Mật khẩu xác nhận không khớp.", success: false);
            return;
        }

        var user = await _userRepo.GetByIdAsync(Session.CurrentUserId);
        if (user == null) return;

        if (Hash(oldPw) != user.PasswordHash)
        {
            ShowResult(PasswordResultText, "Mật khẩu hiện tại không đúng.", success: false);
            return;
        }

        await App.Db.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash WHERE id = @Id",
            new { Hash = Hash(newPw), Id = Session.CurrentUserId });

        OldPasswordBox.Text  = "";
        NewPasswordBox.Text  = "";
        ConfirmPasswordBox.Text = "";

        ShowResult(PasswordResultText, "✓ Đổi mật khẩu thành công", success: true);
    }

    // ─── Đăng xuất ───────────────────────────────────────────────────────────

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        Session.Clear();
        MainWindow.Instance.Navigate(new LoginView());
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    private static void ShowResult(TextBlock label, string msg, bool success)
    {
        label.Text      = msg;
        label.Foreground = new SolidColorBrush(
            success ? Avalonia.Media.Color.Parse("#4CAF50")
                    : Avalonia.Media.Color.Parse("#EF4444"));
        label.IsVisible = true;
    }
}