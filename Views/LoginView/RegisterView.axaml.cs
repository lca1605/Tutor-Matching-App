using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MyApp.Data;
using MyApp.Data.Repositories;


namespace MyApp.Views;

public partial class RegisterView : UserControl
{
    private static readonly Regex UsernameRegex = new(@"^[a-z0-9_]+$", RegexOptions.Compiled);

    private string _selectedRole = "student"; // mặc định học sinh

    public RegisterView()
    {
        InitializeComponent();
    }

    // ─── Role selector ───────────────────────────────────────────────────────

    private void OnRoleStudentPressed(object? sender, PointerPressedEventArgs e)
        => SelectRole("student");

    private void OnRoleTutorPressed(object? sender, PointerPressedEventArgs e)
        => SelectRole("tutor");

    private void SelectRole(string role)
    {
        _selectedRole = role;

        var purple = new SolidColorBrush(Color.Parse("#7C6FCD"));
        var dim    = new SolidColorBrush(Color.Parse("#3A3A5C"));
        var bright = new SolidColorBrush(Color.Parse("#E0E0F0"));
        var muted  = new SolidColorBrush(Color.Parse("#6B6B8D"));

        RoleStudentBorder.BorderBrush   = role == "student" ? purple : dim;
        RoleStudentText.Foreground      = role == "student" ? bright : muted;

        RoleTutorBorder.BorderBrush     = role == "tutor"   ? purple : dim;
        RoleTutorText.Foreground        = role == "tutor"   ? bright : muted;
    }

    // ─── Navigation ──────────────────────────────────────────────────────────

    private void OnBackClick(object? sender, RoutedEventArgs e)
        => MainWindow.Instance.Navigate(new LoginView());

    // ─── Validation realtime ─────────────────────────────────────────────────

    private void OnUsernameChanged(object? sender, TextChangedEventArgs e)
    {
        var text = UsernameBox?.Text ?? "";

        if (string.IsNullOrEmpty(text))
        {
            SetHint(UsernameHint, "Ví dụ: nguyen_van_a, user123", HintState.Neutral);
            SetBoxState(UsernameBox, BoxState.Neutral);
        }
        else if (text.Length < 4)
        {
            SetHint(UsernameHint, "Tối thiểu 4 ký tự", HintState.Error);
            SetBoxState(UsernameBox, BoxState.Invalid);
        }
        else if (!UsernameRegex.IsMatch(text))
        {
            SetHint(UsernameHint, "Chỉ dùng chữ thường a-z, số 0-9 và dấu _", HintState.Error);
            SetBoxState(UsernameBox, BoxState.Invalid);
        }
        else
        {
            SetHint(UsernameHint, "✓ Tên đăng nhập hợp lệ", HintState.Success);
            SetBoxState(UsernameBox, BoxState.Valid);
        }

        UpdateContinueButton();
    }

    private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
    {
        var pw = PasswordBox?.Text ?? "";

        if (string.IsNullOrEmpty(pw))
        {
            SetHint(PasswordHint, " ", HintState.Neutral);
            SetBoxState(PasswordBox, BoxState.Neutral);
        }
        else if (pw.Length < 6)
        {
            SetHint(PasswordHint, "Mật khẩu quá ngắn (tối thiểu 6 ký tự)", HintState.Error);
            SetBoxState(PasswordBox, BoxState.Invalid);
        }
        else
        {
            var strength = GetPasswordStrength(pw);
            SetHint(PasswordHint, strength.Label, strength.State);
            SetBoxState(PasswordBox, strength.State == HintState.Error ? BoxState.Invalid : BoxState.Valid);
        }

        if (!string.IsNullOrEmpty(ConfirmPasswordBox?.Text))
            ValidateConfirm();

        UpdateContinueButton();
    }

    private void OnConfirmPasswordChanged(object? sender, TextChangedEventArgs e)
    {
        ValidateConfirm();
        UpdateContinueButton();
    }

    private void ValidateConfirm()
    {
        var pw      = PasswordBox?.Text ?? "";
        var confirm = ConfirmPasswordBox?.Text ?? "";

        if (string.IsNullOrEmpty(confirm))
        {
            SetHint(ConfirmHint, " ", HintState.Neutral);
            SetBoxState(ConfirmPasswordBox, BoxState.Neutral);
        }
        else if (pw == confirm)
        {
            SetHint(ConfirmHint, "✓ Mật khẩu khớp", HintState.Success);
            SetBoxState(ConfirmPasswordBox, BoxState.Valid);
        }
        else
        {
            SetHint(ConfirmHint, "Mật khẩu không khớp", HintState.Error);
            SetBoxState(ConfirmPasswordBox, BoxState.Invalid);
        }
    }

    // ─── Submit ──────────────────────────────────────────────────────────────

    private async void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox?.Text?.Trim() ?? "";
        var password = PasswordBox?.Text ?? "";
        var confirm  = ConfirmPasswordBox?.Text ?? "";

        if (!IsUsernameValid(username))
        {
            ShowError("Tên đăng nhập không hợp lệ.");
            return;
        }
        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
            return;
        }
        if (password != confirm)
        {
            ShowError("Mật khẩu xác nhận không khớp.");
            return;
        }

        HideError();

        var userRepo = new UserRepository(App.Db);
        if (await userRepo.ExistsAsync(username))
        {
            ShowError("Tên đăng nhập đã tồn tại.");
            return;
        }

        if (_selectedRole == "student")
            MainWindow.Instance.Navigate(new StudentProfileView(username, password));
        else
            MainWindow.Instance.Navigate(new TutorProfileView(username, password));

    }
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private bool IsUsernameValid(string text)
        => text.Length >= 4 && UsernameRegex.IsMatch(text);

    private bool IsPasswordValid()
        => (PasswordBox?.Text?.Length ?? 0) >= 6;

    private bool IsConfirmValid()
        => !string.IsNullOrEmpty(ConfirmPasswordBox?.Text)
           && PasswordBox?.Text == ConfirmPasswordBox?.Text;

    private void UpdateContinueButton()
    {
        if (ContinueButton is null) return;
        ContinueButton.IsEnabled =
            IsUsernameValid(UsernameBox?.Text?.Trim() ?? "") &&
            IsPasswordValid() &&
            IsConfirmValid();
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

    // ─── UI state ────────────────────────────────────────────────────────────

    private enum HintState { Neutral, Success, Error }
    private enum BoxState  { Neutral, Valid,   Invalid }

    private static void SetHint(TextBlock? hint, string text, HintState state)
    {
        if (hint is null) return;
        hint.Text = text;
        hint.Foreground = state switch
        {
            HintState.Success => new SolidColorBrush(Color.Parse("#4CAF50")),
            HintState.Error   => new SolidColorBrush(Color.Parse("#FC5C65")),
            _                 => new SolidColorBrush(Color.Parse("#6B6B8D")),
        };
    }

    private static void SetBoxState(TextBox? box, BoxState state)
    {
        if (box is null) return;
        box.Classes.Remove("valid");
        box.Classes.Remove("invalid");
        if (state == BoxState.Valid)   box.Classes.Add("valid");
        if (state == BoxState.Invalid) box.Classes.Add("invalid");
    }

    private record StrengthResult(string Label, HintState State);

    private static StrengthResult GetPasswordStrength(string pw)
    {
        int score = 0;
        if (pw.Length >= 8)                          score++;
        if (Regex.IsMatch(pw, @"[A-Z]"))             score++;
        if (Regex.IsMatch(pw, @"[0-9]"))             score++;
        if (Regex.IsMatch(pw, @"[^a-zA-Z0-9]"))     score++;

        return score switch
        {
            0 or 1 => new("Mật khẩu yếu", HintState.Error),
            2      => new("Mật khẩu trung bình", HintState.Neutral),
            _      => new("✓ Mật khẩu mạnh", HintState.Success),
        };
    }
}