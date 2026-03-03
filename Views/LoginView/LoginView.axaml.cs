using Avalonia.Controls;
using Avalonia.Interactivity;
using MyApp.Data.Repositories;

namespace MyApp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox?.Text?.Trim() ?? "";
        var password = PasswordBox?.Text ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            return;
        }

    #if DEBUG
        if (username == "admin" && password == "123456")
        {
            Session.CurrentUserId   = 1;
            Session.CurrentUsername = "admin";
            Session.CurrentRole     = "student";
            HideError();
            MainWindow.Instance.Navigate(new HomeView());
            return;
        }
    #endif

        var userRepo = new UserRepository(App.Db);
        var user     = await userRepo.GetByUsernameAsync(username);

        // Kiểm tra user tồn tại VÀ mật khẩu đúng
        if (user == null || HashPassword(password) != user.PasswordHash)
        {
            ShowError("Tên đăng nhập hoặc mật khẩu không đúng.");
            return;
        }

        // Kiểm tra tài khoản bị ban
        if (user.Status == "banned")
        {
            ShowError("Tài khoản của bạn đã bị khóa.");
            return;
        }

        // Tutor chưa được duyệt
        if (user.Status == "pending")
        {
            ShowError("Tài khoản đang chờ admin phê duyệt.");
            return;
        }

        // Gán Session
        Session.CurrentUserId   = user.Id;
        Session.CurrentUsername = user.Username;
        Session.CurrentRole     = user.Role;

        HideError();
        MainWindow.Instance.Navigate(new HomeView());
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.Navigate(new RegisterView());
    }

    private void OnForgotPasswordClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Xử lý quên mật khẩu
    }

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes     = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private void ShowError(string message)
    {
        if (ErrorText  != null) ErrorText.Text      = message;
        if (ErrorPanel != null) ErrorPanel.IsVisible = true;
    }

    private void HideError()
    {
        if (ErrorPanel != null) ErrorPanel.IsVisible = false;
    }
}