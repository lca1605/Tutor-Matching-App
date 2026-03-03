using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class ProfileViewWindow : Window
{
    private readonly int  _targetUserId;
    private          User? _user;

    public ProfileViewWindow() { InitializeComponent(); }

    public ProfileViewWindow(int targetUserId)
    {
        _targetUserId = targetUserId;
        InitializeComponent();
        LoadProfile();
    }

    private async void LoadProfile()
    {
        var userRepo = new UserRepository(App.Db);
        _user = await userRepo.GetByIdAsync(_targetUserId);
        if (_user == null) { Close(); return; }

        // Avatar
        AvatarInitial.Text = _user.AvatarPath == null
            ? "?" : "";
        if (!string.IsNullOrEmpty(_user.AvatarPath) && File.Exists(_user.AvatarPath))
        {
            AvatarImage.Source    = new Bitmap(_user.AvatarPath);
            AvatarImage.IsVisible = true;
            AvatarInitial.IsVisible = false;
        }

        // Ẩn nút nhắn tin nếu xem profile của chính mình
        FooterSection.IsVisible = _targetUserId != Session.CurrentUserId;

        if (_user.Role == "tutor")
            await LoadTutorProfile();
        else
            await LoadStudentProfile();
    }

    private async System.Threading.Tasks.Task LoadTutorProfile()
    {
        var repo    = new ProfileRepository(App.Db);
        var profile = await repo.GetTutorByUserIdAsync(_targetUserId);
        if (profile == null) return;

        DisplayNameText.Text  = profile.FullName;
        AvatarInitial.Text    = profile.FullName.Length > 0
            ? profile.FullName[0].ToString().ToUpper() : "?";
        RoleText.Text         = "Gia sư";
        RoleBadge.Background  = new SolidColorBrush(Color.Parse("#EEE8FF"));
        HourlyRateText.Text   = $"{profile.HourlyRate:N0} đ / giờ";
        TeachingModeText.Text = profile.TeachingMode switch
        {
            "online"  => "Online",
            "offline" => "Offline (trực tiếp)",
            _         => "Online & Offline",
        };
        DegreeText.Text       = string.IsNullOrEmpty(profile.Degree)
            ? "Chưa cập nhật" : profile.Degree;
        DescriptionText.Text  = string.IsNullOrEmpty(profile.Description)
            ? "Chưa có giới thiệu." : profile.Description;

        // Môn dạy
        var subjectRepo = new SubjectRepository(App.Db);
        var subjects    = (await subjectRepo.GetByTutorProfileIdAsync(profile.Id)).ToList();
        SubjectPanel.Children.Clear();
        foreach (var s in subjects)
            SubjectPanel.Children.Add(BuildTag(s.Name));

        TutorInfoSection.IsVisible   = true;
        StudentInfoSection.IsVisible = false;
    }

    private async System.Threading.Tasks.Task LoadStudentProfile()
    {
        var repo    = new ProfileRepository(App.Db);
        var profile = await repo.GetStudentByUserIdAsync(_targetUserId);
        if (profile == null) return;

        DisplayNameText.Text  = profile.DisplayName;
        AvatarInitial.Text    = profile.DisplayName.Length > 0
            ? profile.DisplayName[0].ToString().ToUpper() : "?";
        RoleText.Text         = "Học sinh";
        RoleBadge.Background  = new SolidColorBrush(Color.Parse("#E8F5E9"));
        RoleText.Foreground   = new SolidColorBrush(Color.Parse("#4CAF50"));
        LevelText.Text        = profile.Level;
        BudgetText.Text       = $"{profile.Budget:N0} đ / giờ";
        LearningModeText.Text = profile.LearningMode switch
        {
            "online"  => "Online",
            "offline" => "Offline (trực tiếp)",
            _         => "Online & Offline",
        };
        DescriptionText.Text  = string.IsNullOrEmpty(profile.Description)
            ? "Chưa có giới thiệu." : profile.Description;

        TutorInfoSection.IsVisible   = false;
        StudentInfoSection.IsVisible = true;
    }

    private static Border BuildTag(string text)
        => new Border
        {
            Background   = new SolidColorBrush(Color.Parse("#EEE8FF")),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding      = new Avalonia.Thickness(10, 4),
            Margin       = new Avalonia.Thickness(0, 0, 6, 6),
            Child        = new TextBlock
            {
                Text       = text,
                FontSize   = 12,
                Foreground = new SolidColorBrush(Color.Parse("#7C6FCD")),
            }
        };

    private async void OnMessageClick(object? sender, RoutedEventArgs e)
    {
        if (_user == null) return;

        // Xác định student/tutor
        int studentId, tutorId;
        if (Session.CurrentRole == "student")
        {
            studentId = Session.CurrentUserId;
            tutorId   = _targetUserId;
        }
        else
        {
            studentId = _targetUserId;
            tutorId   = Session.CurrentUserId;
        }

        var repo   = new MessageRepository(App.Db);
        var convId = await repo.GetOrCreateConversationAsync(studentId, tutorId);
        var convs  = (await repo.GetConversationsAsync(Session.CurrentUserId)).ToList();
        var conv   = convs.FirstOrDefault(c => c.Id == convId);

        Close();

        if (conv != null)
            MainWindow.Instance.Navigate(new ChatView(conv, Session.CurrentUserId));
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}