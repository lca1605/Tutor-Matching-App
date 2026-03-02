using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MyApp.Data;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class TutorProfileView : UserControl
{
    private readonly string _username;
    private readonly string _password;

    private List<Subject> _allSubjects       = new();
    private readonly List<string> _selectedSubjects   = new();
    private readonly List<int>    _selectedSubjectIds = new();
    private string  _teachingMode = "online";
    private string? _cvFilePath;

    // ─── Constructors ─────────────────────────────────────────────────────────

    public TutorProfileView()
    {
        _username = string.Empty;
        _password = string.Empty;
        InitializeComponent();
        LoadSubjects();
    }

    public TutorProfileView(string username, string password)
    {
        _username = username;
        _password = password;
        InitializeComponent();
        LoadSubjects();
    }

    // ─── Load subjects từ DB ──────────────────────────────────────────────────

    private async void LoadSubjects()
    {
        var repo = new SubjectRepository(App.Db);
        _allSubjects = (await repo.GetAllActiveAsync()).ToList();
        SubjectComboBox.ItemsSource = _allSubjects.Select(s => s.Name).ToList();
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    private void OnBackClick(object? sender, RoutedEventArgs e)
        => MainWindow.Instance.Navigate(new RegisterView());

    // ─── Subject tags ─────────────────────────────────────────────────────────

    private void OnSubjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        var selectedName = SubjectComboBox.SelectedItem as string;
        if (selectedName == null) return;

        var subject = _allSubjects.FirstOrDefault(s => s.Name == selectedName);
        if (subject == null || _selectedSubjectIds.Contains(subject.Id))
        {
            SubjectComboBox.SelectedItem = null;
            return;
        }

        _selectedSubjectIds.Add(subject.Id);
        _selectedSubjects.Add(subject.Name);
        AddSubjectTag(subject.Name);
        SubjectComboBox.SelectedItem = null;
    }

    private void AddSubjectTag(string subject)
    {
        var tag = new Border
        {
            CornerRadius    = new Avalonia.CornerRadius(20),
            Background      = new SolidColorBrush(Avalonia.Media.Color.Parse("#2A1A4A")),
            BorderBrush     = new SolidColorBrush(Avalonia.Media.Color.Parse("#7C6FCD")),
            BorderThickness = new Avalonia.Thickness(1),
            Padding         = new Avalonia.Thickness(10, 4),
            Margin          = new Avalonia.Thickness(0, 0, 6, 6),
            Tag             = subject,
        };

        var panel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing     = 6,
        };

        panel.Children.Add(new TextBlock
        {
            Text              = subject,
            Foreground        = new SolidColorBrush(Avalonia.Media.Color.Parse("#C0B8F0")),
            FontSize          = 12,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        });

        var removeBtn = new Button
        {
            Content         = "×",
            Background      = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            Padding         = new Avalonia.Thickness(0),
            Foreground      = new SolidColorBrush(Avalonia.Media.Color.Parse("#7C6FCD")),
            FontSize        = 15,
            Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            Tag             = subject,
        };
        removeBtn.Click += OnRemoveSubjectClick;
        panel.Children.Add(removeBtn);

        tag.Child = panel;
        SubjectTagPanel.Children.Add(tag);
    }

    private void OnRemoveSubjectClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string subject) return;

        var subjectObj = _allSubjects.FirstOrDefault(s => s.Name == subject);
        if (subjectObj != null) _selectedSubjectIds.Remove(subjectObj.Id);
        _selectedSubjects.Remove(subject);

        var toRemove = SubjectTagPanel.Children
            .OfType<Border>()
            .FirstOrDefault(b => b.Tag as string == subject);
        if (toRemove != null)
            SubjectTagPanel.Children.Remove(toRemove);
    }

    // ─── Teaching mode ────────────────────────────────────────────────────────

    private void OnModeOnlineClick(object? sender, RoutedEventArgs e)  => SetMode("online");
    private void OnModeOfflineClick(object? sender, RoutedEventArgs e) => SetMode("offline");
    private void OnModeBothClick(object? sender, RoutedEventArgs e)    => SetMode("both");

    private void SetMode(string mode)
    {
        _teachingMode = mode;

        ModeOnlineBtn.Classes.Remove("mode-on");  ModeOnlineBtn.Classes.Remove("mode-off");
        ModeOfflineBtn.Classes.Remove("mode-on"); ModeOfflineBtn.Classes.Remove("mode-off");
        ModeBothBtn.Classes.Remove("mode-on");    ModeBothBtn.Classes.Remove("mode-off");

        ModeOnlineBtn.Classes.Add(mode  == "online"  ? "mode-on" : "mode-off");
        ModeOfflineBtn.Classes.Add(mode == "offline" ? "mode-on" : "mode-off");
        ModeBothBtn.Classes.Add(mode    == "both"    ? "mode-on" : "mode-off");
    }

    // ─── Pick CV ──────────────────────────────────────────────────────────────

    private async void OnPickCvClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = "Chọn file CV",
            AllowMultiple  = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("CV") { Patterns = new[] { "*.pdf", "*.doc", "*.docx" } },
            }
        });

        if (files.Count > 0)
        {
            _cvFilePath           = files[0].Path.LocalPath;
            CvFileNameText.Text   = files[0].Name;
            CvFileNameText.Foreground = new SolidColorBrush(Avalonia.Media.Color.Parse("#C0B8F0"));
        }
    }

    // ─── Submit ───────────────────────────────────────────────────────────────

    private async void OnSubmitClick(object? sender, RoutedEventArgs e)
    {
        var fullName   = FullNameBox?.Text?.Trim()    ?? "";
        var desc       = DescriptionBox?.Text?.Trim() ?? "";
        var workplace  = WorkplaceBox?.Text?.Trim()   ?? "";
        var profession = ProfessionBox?.Text?.Trim()  ?? "";
        var degree     = DegreeBox?.Text?.Trim()      ?? "";
        var rateText   = HourlyRateBox?.Text?.Trim()  ?? "";

        if (string.IsNullOrEmpty(fullName))
        {
            ShowError("Vui lòng nhập họ và tên."); return;
        }
        if (string.IsNullOrEmpty(workplace))
        {
            ShowError("Vui lòng nhập nơi làm/học."); return;
        }
        if (string.IsNullOrEmpty(profession))
        {
            ShowError("Vui lòng nhập ngành nghề chính."); return;
        }
        if (_selectedSubjectIds.Count == 0)
        {
            ShowError("Vui lòng chọn ít nhất 1 môn học."); return;
        }
        if (!decimal.TryParse(rateText, out var rate) || rate <= 0)
        {
            ShowError("Vui lòng nhập học phí hợp lệ (số dương)."); return;
        }

        HideError();
        SubmitButton.IsEnabled = false;

        var userRepo    = new UserRepository(App.Db);
        var profileRepo = new ProfileRepository(App.Db);

        var userId = await userRepo.CreateAsync(_username, HashPassword(_password), "tutor", "pending");

        var profileId = await profileRepo.SaveTutorAsync(new TutorProfile
        {
            UserId       = userId,
            FullName     = fullName,
            Description  = desc,
            Workplace    = workplace,
            Profession   = profession,
            Degree       = degree,
            CvFilePath   = _cvFilePath,
            HourlyRate   = rate,
            TeachingMode = _teachingMode,
            AdminStatus  = "pending",
        });

        await profileRepo.SaveTutorSubjectsAsync(profileId, _selectedSubjectIds);

        PendingOverlay.IsVisible = true;
    }

    private void OnGoToLoginClick(object? sender, RoutedEventArgs e)
        => MainWindow.Instance.Navigate(new LoginView());

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string HashPassword(string password)
    {
        using var sha   = System.Security.Cryptography.SHA256.Create();
        var bytes       = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private void ShowError(string msg)
    {
        if (ErrorText  != null) ErrorText.Text      = msg;
        if (ErrorPanel != null) ErrorPanel.IsVisible = true;
    }

    private void HideError()
    {
        if (ErrorPanel != null) ErrorPanel.IsVisible = false;
    }
}