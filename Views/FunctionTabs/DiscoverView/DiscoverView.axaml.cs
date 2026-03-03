using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia;
using Avalonia.VisualTree;
using MyApp.Data.Repositories;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Views;

public partial class DiscoverView : UserControl
{
    private readonly TutorQueueService _queue = new();
    private bool _isLoading = false;

    public DiscoverView()
    {
        InitializeComponent();
        LoadByRole();
    }

    private async void LoadByRole()
    {
        if (Session.CurrentRole == "tutor")
            LoadTutorMode();
        else
            await LoadStudentMode();
    }

    // ─── Student mode ─────────────────────────────────────────────────────────

    private async Task LoadStudentMode()
    {
        StudentListSection.IsVisible = false;
        AnimateSectionIn(TutorCardSection);
        FilterPill.IsVisible         = true;

        CardCountText.Text = "Gợi ý hôm nay";
        CardSubText.Text   = "Gia sư phù hợp với bạn";

        await _queue.ApplyFilterAsync();
        UpdateCardCount();

        // Vòng lặp liên tục — mỗi lần await cho đến khi user swipe xong
        while (true)
        {
            if (_isLoading) break;
            _isLoading = true;

            var tutor = await _queue.NextAsync();
            if (tutor == null)
            {
                ShowEmpty();
                _isLoading = false;
                break;
            }

            // Tạo card mới và đặt vào CardHost
            var card = new TutorCardView(tutor);
            CardHost.Content = card;
            _isLoading       = false;

            // Await swipe — block ở đây cho đến khi user swipe xong
            var result = await card.WaitForSwipeAsync();

            // Xử lý kết quả sau khi animation bay ra XONG hoàn toàn
            if (result == SwipeResult.Interest)
            {
                var notiRepo = new NotificationRepository(App.Db);
                await notiRepo.StudentInterestAsync(
                    studentId:   Session.CurrentUserId,
                    tutorId:     card.CurrentTutorId,
                    studentName: Session.CurrentUsername);
            }
            // SwipeResult.Reject — không cần làm gì, load card tiếp
        }
    }

    private void ShowEmpty()
    {
        CardHost.Content = new TextBlock
        {
            Text                = "Không tìm thấy gia sư phù hợp",
            FontSize            = 14,
            Foreground          = new SolidColorBrush(Color.Parse("#9090A8")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center,
        };
    }

    private void UpdateCardCount()
    {
        CardCountText.Text = _queue.TotalCount > 0
            ? $"{_queue.TotalCount} gia sư phù hợp"
            : "Gợi ý hôm nay";
    }

    // ─── Filter ───────────────────────────────────────────────────────────────

    private void OnFilterClick(object? sender, RoutedEventArgs e)
    {
        FilterHost.Content      = new FilterPopupView(OnFilterApplied, OnFilterClosed);
        FilterOverlay.IsVisible = true;
    }

    private async void OnFilterApplied(FilterOptions options)
    {
        FilterOverlay.IsVisible = false;

        // Reset và restart vòng lặp swipe
        await _queue.ApplyFilterAsync(options);
        UpdateCardCount();

        // Huỷ card hiện tại nếu có bằng cách set null
        CardHost.Content = null;
        _isLoading       = false;

        await LoadStudentMode();
    }

    private void OnFilterClosed()
        => FilterOverlay.IsVisible = false;

    // ─── Tutor mode ───────────────────────────────────────────────────────────

    private async void LoadTutorMode()
    {
        TutorCardSection.IsVisible = false;
        AnimateSectionIn(StudentListSection);
        FilterPill.IsVisible         = false;

        CardCountText.Text = "Học sinh quan tâm";
        CardSubText.Text   = "Những học sinh yêu thích hồ sơ của bạn";

        var students = await GetInterestedStudentsAsync();

        if (!students.Any())
        {
            EmptyStudentPanel.IsVisible  = true;
            StudentListScroll.IsVisible  = false;
            return;
        }

        EmptyStudentPanel.IsVisible  = false;
        StudentListScroll.IsVisible  = true;
        StudentList.Children.Clear();

        foreach (var s in students)
            StudentList.Children.Add(BuildStudentItem(s));
    }

    private async Task<IEnumerable<StudentProfile>> GetInterestedStudentsAsync()
    {
        var rows = await App.Db.QueryAsync<dynamic>(@"
            SELECT si.student_id
            FROM student_interests si
            WHERE si.tutor_id = @TutorId
            ORDER BY si.created_at DESC",
            new { TutorId = Session.CurrentUserId });

        var profileRepo = new ProfileRepository(App.Db);
        var result      = new List<StudentProfile>();

        foreach (var row in rows)
        {
            var profile = await profileRepo.GetStudentByUserIdAsync((int)row.student_id);
            if (profile != null) result.Add(profile);
        }

        return result;
    }

    private Control BuildStudentItem(StudentProfile profile)
    {
        var btn   = new Button { Classes = { "student-card" } };
        btn.Tag    = profile.UserId;
        btn.Click += OnStudentItemClick;

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("52,*"),
            Margin            = new Avalonia.Thickness(16, 12),
        };

        var avatar = new Border
        {
            Width        = 44,
            Height       = 44,
            CornerRadius = new Avalonia.CornerRadius(22),
            Background   = new SolidColorBrush(Color.Parse("#E8F5E9")),
            Child        = new TextBlock
            {
                Text                = profile.DisplayName.Length > 0
                    ? profile.DisplayName[0].ToString().ToUpper() : "?",
                FontSize            = 18,
                FontWeight          = FontWeight.Bold,
                Foreground          = new SolidColorBrush(Color.Parse("#4CAF50")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            }
        };
        Grid.SetColumn(avatar, 0);
        grid.Children.Add(avatar);

        var info = new StackPanel
        {
            Spacing           = 3,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Avalonia.Thickness(10, 0, 0, 0),
        };
        info.Children.Add(new TextBlock
        {
            Text       = profile.DisplayName,
            FontSize   = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#111122")),
        });
        info.Children.Add(new TextBlock
        {
            Text       = $"{profile.Level}  •  {profile.Budget:N0}đ/giờ",
            FontSize   = 12,
            Foreground = new SolidColorBrush(Color.Parse("#9090A8")),
        });
        info.Children.Add(new TextBlock
        {
            Text       = profile.LearningMode switch
            {
                "online"  => "Online",
                "offline" => "Offline",
                _         => "Online & Offline",
            },
            FontSize   = 11,
            Foreground = new SolidColorBrush(Color.Parse("#7C6FCD")),
        });
        Grid.SetColumn(info, 1);
        grid.Children.Add(info);

        var wrapper = new StackPanel { Spacing = 0 };
        wrapper.Children.Add(grid);
        wrapper.Children.Add(new Border
        {
            Height     = 1,
            Background = new SolidColorBrush(Color.Parse("#F0F0F5")),
            Margin     = new Avalonia.Thickness(16, 0),
        });

        btn.Content = wrapper;
        return btn;
    }

    private async void OnStudentItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int userId) return;
        var window = new ProfileViewWindow(userId);
        await window.ShowDialog(MainWindow.Instance);
    }

    private void AnimateSectionIn(Control section)
    {
        section.IsVisible = true;

        section.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        var scale = new ScaleTransform(0.92, 0.92);
        section.RenderTransform = scale;
        section.Opacity = 0;

        section.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(220),
                Easing = new CubicEaseOut()
            },
            new DoubleTransition
            {
                Property = ScaleTransform.ScaleXProperty,
                Duration = TimeSpan.FromMilliseconds(260),
                Easing = new CubicEaseOut()
            },
            new DoubleTransition
            {
                Property = ScaleTransform.ScaleYProperty,
                Duration = TimeSpan.FromMilliseconds(260),
                Easing = new CubicEaseOut()
            }
        };

        // Trigger animation
        section.Opacity = 1;
        scale.ScaleX = 1;
        scale.ScaleY = 1;
    }
}