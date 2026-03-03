using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class EditProfileWindow : Window
{
    private string _studentMode = "online";
    private string _tutorMode   = "online";
    private readonly List<(int SubjectId, string Name, CheckBox Box)> _subjectBoxes = new();

    private static readonly List<string> Levels = new()
    {
        "Lớp 1","Lớp 2","Lớp 3","Lớp 4","Lớp 5",
        "Lớp 6","Lớp 7","Lớp 8","Lớp 9",
        "Lớp 10","Lớp 11","Lớp 12",
        "Năm 1 Đại học","Năm 2 Đại học","Năm 3 Đại học","Năm 4 Đại học",
    };

    public EditProfileWindow() { InitializeComponent(); }

    public EditProfileWindow(int userId)
    {
        InitializeComponent();
        LevelCombo.ItemsSource = Levels;
        LoadProfile(userId);
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    private async void LoadProfile(int userId)
    {
        var userRepo = new UserRepository(App.Db);
        var user     = await userRepo.GetByIdAsync(userId);
        if (user == null) return;

        // Avatar
        if (!string.IsNullOrEmpty(user.AvatarPath) && File.Exists(user.AvatarPath))
        {
            AvatarImage.Source      = new Bitmap(user.AvatarPath);
            AvatarImage.IsVisible   = true;
            AvatarInitial.IsVisible = false;
        }

        if (user.Role == "student")
        {
            StudentForm.IsVisible = true;
            TutorForm.IsVisible   = false;
            await LoadStudentData(userId);
        }
        else
        {
            StudentForm.IsVisible = false;
            TutorForm.IsVisible   = true;
            await LoadTutorData(userId);
        }
    }

    private async Task LoadStudentData(int userId)
    {
        var repo    = new ProfileRepository(App.Db);
        var profile = await repo.GetStudentByUserIdAsync(userId);
        if (profile == null) return;

        StudentNameBox.Text         = profile.DisplayName;
        BudgetBox.Text              = profile.Budget.ToString("0");
        RequirementBox.Text         = profile.Requirement;
        LevelCombo.SelectedItem     = profile.Level;
        AvatarInitial.Text          = profile.DisplayName.Length > 0
            ? profile.DisplayName[0].ToString().ToUpper() : "?";

        SetStudentMode(profile.LearningMode);
    }

    private async Task LoadTutorData(int userId)
    {
        var repo    = new ProfileRepository(App.Db);
        var profile = await repo.GetTutorByUserIdAsync(userId);
        if (profile == null) return;

        TutorNameBox.Text    = profile.FullName;
        DegreeBox.Text       = profile.Degree;
        HourlyRateBox.Text   = profile.HourlyRate.ToString("0");
        DescriptionBox.Text  = profile.Description;
        AvatarInitial.Text   = profile.FullName.Length > 0
            ? profile.FullName[0].ToString().ToUpper() : "?";

        SetTutorMode(profile.TeachingMode);

        // Load checkbox môn dạy
        var subjectRepo  = new SubjectRepository(App.Db);
        var allSubjects  = (await subjectRepo.GetAllActiveAsync()).ToList();
        var mySubjects   = (await subjectRepo.GetByTutorProfileIdAsync(profile.Id))
                            .Select(s => s.Id).ToHashSet();

        SubjectCheckPanel.Children.Clear();
        _subjectBoxes.Clear();

        foreach (var s in allSubjects)
        {
            var cb = new CheckBox
            {
                Content   = s.Name,
                IsChecked = mySubjects.Contains(s.Id),
                Margin    = new Avalonia.Thickness(0, 0, 12, 8),
                Foreground = new SolidColorBrush(Color.Parse("#333344")),
                FontSize  = 13,
            };
            _subjectBoxes.Add((s.Id, s.Name, cb));
            SubjectCheckPanel.Children.Add(cb);
        }
    }

    // ─── Avatar ───────────────────────────────────────────────────────────────

    private async void OnChangeAvatarClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title          = "Chọn ảnh đại diện",
                AllowMultiple  = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Ảnh")
                    {
                        Patterns = new[] { "*.jpg","*.jpeg","*.png","*.webp" }
                    }
                }
            });

        if (files.Count == 0) return;

        var localPath = files[0].Path.LocalPath;
        var ext       = Path.GetExtension(localPath).ToLower();
        var savedName = $"avatar_{Session.CurrentUserId}{ext}";
        var destPath  = Path.Combine(MessageRepository.FileStorageRoot, savedName);

        File.Copy(localPath, destPath, overwrite: true);

        await App.Db.ExecuteAsync(
            "UPDATE users SET avatar_path = @Path WHERE id = @Id",
            new { Path = destPath, Id = Session.CurrentUserId });

        AvatarImage.Source      = new Bitmap(destPath);
        AvatarImage.IsVisible   = true;
        AvatarInitial.IsVisible = false;
    }

    // ─── Mode buttons ─────────────────────────────────────────────────────────

    private void SetStudentMode(string mode)
    {
        _studentMode = mode;
        SModeOnlineBtn.Classes.Clear();
        SModeOfflineBtn.Classes.Clear();
        SModeBothBtn.Classes.Clear();
        SModeOnlineBtn.Classes.Add(mode  == "online"  ? "mode-on" : "mode-off");
        SModeOfflineBtn.Classes.Add(mode == "offline" ? "mode-on" : "mode-off");
        SModeBothBtn.Classes.Add(mode   == "both"    ? "mode-on" : "mode-off");
    }

    private void SetTutorMode(string mode)
    {
        _tutorMode = mode;
        TModeOnlineBtn.Classes.Clear();
        TModeOfflineBtn.Classes.Clear();
        TModeBothBtn.Classes.Clear();
        TModeOnlineBtn.Classes.Add(mode  == "online"  ? "mode-on" : "mode-off");
        TModeOfflineBtn.Classes.Add(mode == "offline" ? "mode-on" : "mode-off");
        TModeBothBtn.Classes.Add(mode   == "both"    ? "mode-on" : "mode-off");
    }

    private void OnStudentModeOnlineClick(object? sender, RoutedEventArgs e)  => SetStudentMode("online");
    private void OnStudentModeOfflineClick(object? sender, RoutedEventArgs e) => SetStudentMode("offline");
    private void OnStudentModeBothClick(object? sender, RoutedEventArgs e)    => SetStudentMode("both");
    private void OnTutorModeOnlineClick(object? sender, RoutedEventArgs e)    => SetTutorMode("online");
    private void OnTutorModeOfflineClick(object? sender, RoutedEventArgs e)   => SetTutorMode("offline");
    private void OnTutorModeBothClick(object? sender, RoutedEventArgs e)      => SetTutorMode("both");

    // ─── Save ─────────────────────────────────────────────────────────────────

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var userRepo = new UserRepository(App.Db);
        var user     = await userRepo.GetByIdAsync(Session.CurrentUserId);
        if (user == null) return;

        if (user.Role == "student")
            await SaveStudent();
        else
            await SaveTutor();
    }

    private async Task SaveStudent()
    {
        var name   = StudentNameBox.Text?.Trim() ?? "";
        var level  = LevelCombo.SelectedItem as string;
        var budget = BudgetBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(name))
        {
            ShowResult("Vui lòng nhập tên hiển thị.", success: false); return;
        }
        if (level == null)
        {
            ShowResult("Vui lòng chọn trình độ.", success: false); return;
        }
        if (!decimal.TryParse(budget, out var budgetVal) || budgetVal <= 0)
        {
            ShowResult("Vui lòng nhập ngân sách hợp lệ.", success: false); return;
        }

        await App.Db.ExecuteAsync(@"
            UPDATE student_profiles
            SET display_name  = @Name,
                level         = @Level,
                budget        = @Budget,
                learning_mode = @Mode,
                requirement   = @Req
            WHERE user_id = @UserId",
            new
            {
                Name   = name,
                Level  = level,
                Budget = budgetVal,
                Mode   = _studentMode,
                Req    = RequirementBox.Text?.Trim() ?? "",
                UserId = Session.CurrentUserId,
            });

        ShowResult("✓ Đã lưu hồ sơ", success: true);
    }

    private async Task SaveTutor()
    {
        var name   = TutorNameBox.Text?.Trim()   ?? "";
        var degree = DegreeBox.Text?.Trim()       ?? "";
        var rate   = HourlyRateBox.Text?.Trim()   ?? "";
        var desc   = DescriptionBox.Text?.Trim()  ?? "";

        if (string.IsNullOrEmpty(name))
        {
            ShowResult("Vui lòng nhập họ và tên.", success: false); return;
        }
        if (!decimal.TryParse(rate, out var rateVal) || rateVal <= 0)
        {
            ShowResult("Vui lòng nhập học phí hợp lệ.", success: false); return;
        }

        await App.Db.ExecuteAsync(@"
            UPDATE tutor_profiles
            SET full_name     = @Name,
                degree        = @Degree,
                hourly_rate   = @Rate,
                teaching_mode = @Mode,
                description   = @Desc
            WHERE user_id = @UserId",
            new
            {
                Name   = name,
                Degree = degree,
                Rate   = rateVal,
                Mode   = _tutorMode,
                Desc   = desc,
                UserId = Session.CurrentUserId,
            });

        // Lưu môn dạy
        var profileRepo = new ProfileRepository(App.Db);
        var profile     = await profileRepo.GetTutorByUserIdAsync(Session.CurrentUserId);
        if (profile != null)
        {
            var selectedIds = _subjectBoxes
                .Where(x => x.Box.IsChecked == true)
                .Select(x => x.SubjectId)
                .ToList();

            await App.Db.ExecuteAsync(
                "DELETE FROM tutor_subjects WHERE tutor_profile_id = @Id",
                new { Id = profile.Id });

            var subjectRepo = new SubjectRepository(App.Db);
            await subjectRepo.SaveTutorSubjectsAsync(profile.Id, selectedIds);
        }

        ShowResult("✓ Đã lưu hồ sơ", success: true);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void ShowResult(string msg, bool success)
    {
        ResultText.Text      = msg;
        ResultText.Foreground = new SolidColorBrush(
            success ? Color.Parse("#4CAF50") : Color.Parse("#EF4444"));
        ResultText.IsVisible = true;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}