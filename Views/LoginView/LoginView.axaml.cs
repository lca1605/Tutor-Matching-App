using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MyApp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox?.Text?.Trim() ?? "";
        var password = PasswordBox?.Text ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            return;
        }

        // TODO: Thay bằng logic xác thực thực tế (service/DB)
        if (username == "admin" && password == "123456")
        {
            HideError();
            //MainWindow.Instance.Navigate(new HomeView());
        }
        else
        {
            ShowError("Tên đăng nhập hoặc mật khẩu không đúng.");
        }
    }

    private void OnRegisterClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.Navigate(new RegisterView());
    }

    private void OnForgotPasswordClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Xử lý quên mật khẩu
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