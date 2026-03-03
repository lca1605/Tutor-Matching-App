using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using MyApp.Models;

namespace MyApp.Views;

public enum SwipeResult { Interest, Reject }

public partial class TutorCardView : UserControl
{
    private Point  _dragStart;
    private bool   _isDragging;
    private double _currentX;
    private bool   _animating;

    public int CurrentTutorId { get; private set; }

    private const double SwipeThreshold = 80;
    private const double FlyDistance    = 650;
    private const double MaxRotation    = 15;

    // DiscoverView await cái này để biết swipe xong chưa và kết quả là gì
    private TaskCompletionSource<SwipeResult>? _swipeTcs;

    // ─── Constructor ──────────────────────────────────────────────────────────

    public TutorCardView()
    {
        InitializeComponent();
        ShowPlaceholder();
        AttachEvents();
    }

    public TutorCardView(TutorProfile tutor)
    {
        InitializeComponent();
        LoadTutor(tutor);
        AttachEvents();

        // Ẩn trước — WaitForSwipeAsync sẽ gọi AnimateEnter
        CardBorder.Opacity = 0;
        CardBorder.RenderTransformOrigin =
            new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        CardBorder.RenderTransform = new ScaleTransform(0.90, 0.90);
    }

    // ─── API cho DiscoverView ─────────────────────────────────────────────────

    /// <summary>
    /// Set card vào CardHost rồi gọi hàm này.
    /// Await để chờ user swipe xong — trả về Interest hoặc Reject.
    /// </summary>
    public async Task<SwipeResult> WaitForSwipeAsync()
    {
        _swipeTcs = new TaskCompletionSource<SwipeResult>();

        // Chờ layout xong rồi mới animate in
        await Task.Delay(30);
        await AnimateEnter();

        // Chờ user swipe
        return await _swipeTcs.Task;
    }

    // ─── Events ───────────────────────────────────────────────────────────────

    private void AttachEvents()
    {
        CardBorder.PointerPressed  += OnPointerPressed;
        CardBorder.PointerMoved    += OnPointerMoved;
        CardBorder.PointerReleased += OnPointerReleased;
        KeyDown += OnKeyDown;
        Loaded  += (_, _) => Focus();
    }

    // ─── Load data ────────────────────────────────────────────────────────────

    public void LoadTutor(TutorProfile tutor, double rating = 0, int reviewCount = 0)
    {
        CurrentTutorId        = tutor.UserId;
        TutorNameText.Text    = tutor.FullName;
        TutorTitleText.Text   = tutor.Profession;
        DegreeText.Text       = string.IsNullOrEmpty(tutor.Degree)     ? "--" : tutor.Degree;
        ExperienceText.Text   = string.IsNullOrEmpty(tutor.Workplace)  ? "--" : tutor.Workplace;
        PriceText.Text        = $"{tutor.HourlyRate:N0}đ/giờ";
        DescriptionText.Text  = tutor.Description;
        RatingText.Text       = rating > 0 ? rating.ToString("F1") : "--";
        ReviewCountText.Text  = reviewCount > 0 ? $"{reviewCount} reviews" : "Chưa có đánh giá";
        ModeText.Text         = tutor.TeachingMode switch
        {
            "online"  => "Online",
            "offline" => "Offline (dạy tại nhà)",
            "both"    => "Online & Offline",
            _         => "--"
        };
        LoadSubjectTags(tutor.Subjects);
    }

    private void LoadSubjectTags(List<Subject> subjects)
    {
        SubjectTagPanel.Children.Clear();
        foreach (var s in subjects)
            SubjectTagPanel.Children.Add(new Border
            {
                Background   = new SolidColorBrush(Color.Parse("#F0EDFF")),
                CornerRadius = new CornerRadius(12),
                Padding      = new Thickness(10, 4),
                Margin       = new Thickness(0, 0, 6, 6),
                Child        = new TextBlock
                {
                    Text       = s.Name,
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#7C6FCD")),
                    FontWeight = FontWeight.SemiBold,
                }
            });
    }

    private void ShowPlaceholder()
    {
        TutorNameText.Text   = "Đang tải gia sư...";
        DegreeText.Text      = "--";
        ExperienceText.Text  = "--";
        PriceText.Text       = "--";
        ModeText.Text        = "--";
        RatingText.Text      = "--";
        ReviewCountText.Text = "--";
        DescriptionText.Text = "";
    }

    // ─── Pointer ──────────────────────────────────────────────────────────────

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_animating || _swipeTcs == null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragStart  = e.GetPosition(this);
        _isDragging = false;
        _currentX   = 0;
        e.Pointer.Capture(CardBorder);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_animating || _swipeTcs == null) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var delta = e.GetPosition(this).X - _dragStart.X;
        if (Math.Abs(delta) > 5) _isDragging = true;
        if (!_isDragging) return;

        var rubberDelta = ApplyRubberBand(delta);
        _currentX = rubberDelta;
        ApplyDragTransform(rubberDelta);
        UpdateHints(delta);
    }

    private static double ApplyRubberBand(double delta)
    {
        const double threshold = 120;
        const double resistance = 0.25;

        var sign = Math.Sign(delta);
        var abs  = Math.Abs(delta);

        if (abs <= threshold)
            return delta;

        var over     = abs - threshold;
        var dampened = threshold + over * resistance;
        return sign * dampened;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_animating || _swipeTcs == null) return;
        var delta = e.GetPosition(this).X - _dragStart.X;

        if (!_isDragging && Math.Abs(delta) < 5)
        {
            OpenProfile();
            return;
        }

        _isDragging = false;

        if (delta > SwipeThreshold)       _ = ConfirmSwipe(right: true);
        else if (delta < -SwipeThreshold) _ = ConfirmSwipe(right: false);
        else                               _ = AnimateReturn();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_animating || _swipeTcs == null) return;
        switch (e.Key)
        {
            case Key.Right: _ = ConfirmSwipe(right: true);  break;
            case Key.Left:  _ = ConfirmSwipe(right: false); break;
            case Key.Enter:
            case Key.Space: OpenProfile(); break;
        }
    }

    // ─── Confirm swipe — bay ra rồi set TCS ───────────────────────────────────

    private async Task ConfirmSwipe(bool right)
    {
        _animating = true;
        await AnimateFlyOut(right);
        _animating = false;

        // Set kết quả — DiscoverView đang await WaitForSwipeAsync() sẽ tiếp tục
        _swipeTcs?.TrySetResult(right ? SwipeResult.Interest : SwipeResult.Reject);
    }

    // ─── Transform helpers ────────────────────────────────────────────────────

    private void ApplyDragTransform(double deltaX)
    {
        var w        = Math.Max(CardBorder.Bounds.Width, 300);
        var rotation = (deltaX / w) * MaxRotation;
        var liftY    = -Math.Abs(deltaX) * 0.04;
        CardBorder.RenderTransformOrigin =
            new RelativePoint(0.5, 1.0, RelativeUnit.Relative);
        CardBorder.RenderTransform = new TransformGroup
        {
            Children = new Transforms
            {
                new RotateTransform(rotation),
                new TranslateTransform(deltaX, liftY),
            }
        };
    }

    private void UpdateHints(double rawDelta)
    {
        var ratio     = Math.Clamp(Math.Abs(rawDelta) / SwipeThreshold, 0, 1);
        HintRight.Opacity = rawDelta > 0 ? ratio : 0;
        HintLeft.Opacity  = rawDelta < 0 ? ratio : 0;
    }

    // ─── Animations ───────────────────────────────────────────────────────────

    private async Task AnimateFlyOut(bool right)
    {
        var targetX  = right ? FlyDistance : -FlyDistance;
        var targetY  = -80.0;
        var rotation = right ? MaxRotation * 1.3 : -MaxRotation * 1.3;
        var startX   = _currentX;
        var startRot = (startX / Math.Max(CardBorder.Bounds.Width, 300)) * MaxRotation;
        var steps    = 28;

        for (int i = 0; i <= steps; i++)
        {
            var t    = (double)i / steps;
            var ease = t * t * t; // ease in cubic
            var dx   = startX + (targetX - startX) * ease;
            var dy   = targetY * ease;
            var r    = startRot + (rotation - startRot) * ease;

            CardBorder.RenderTransformOrigin =
                new RelativePoint(0.5, 1.0, RelativeUnit.Relative);
            CardBorder.RenderTransform = new TransformGroup
            {
                Children = new Transforms
                {
                    new RotateTransform(r),
                    new TranslateTransform(dx, dy),
                }
            };
            CardBorder.Opacity = Math.Clamp(1 - ease * 0.8, 0, 1);
            HintRight.Opacity  = right  ? Math.Clamp(ease * 2, 0, 1) : 0;
            HintLeft.Opacity   = !right ? Math.Clamp(ease * 2, 0, 1) : 0;

            await Task.Delay(12);
        }

        // Ẩn hoàn toàn trước khi báo xong
        CardBorder.Opacity = 0;
        HintLeft.Opacity   = 0;
        HintRight.Opacity  = 0;
        _currentX          = 0;
    }

    private async Task AnimateReturn()
    {
        _animating = true;
        var startX = _currentX;
        var steps  = 22;

        for (int i = 0; i <= steps; i++)
        {
            var t    = (double)i / steps;
            var ease = 1 - Math.Pow(1 - t, 3); // ease out cubic
            ApplyDragTransform(startX * (1 - ease));
            UpdateHints(startX * (1 - ease));
            await Task.Delay(10);
        }

        CardBorder.RenderTransform = null;
        HintLeft.Opacity           = 0;
        HintRight.Opacity          = 0;
        _currentX                  = 0;
        _animating                 = false;
    }

    private async Task AnimateEnter()
    {
        var steps = 26;
        for (int i = 0; i <= steps; i++)
        {
            var t    = (double)i / steps;
            var c1   = 1.70158;
            var c3   = c1 + 1;
            // ease out back
            var ease = 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
            var scale = Math.Clamp(0.90 + 0.10 * ease, 0.88, 1.02);

            CardBorder.RenderTransformOrigin =
                new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            CardBorder.RenderTransform = new ScaleTransform(scale, scale);
            CardBorder.Opacity         = Math.Clamp(t * 2.2, 0, 1);

            await Task.Delay(12);
        }

        CardBorder.RenderTransform = null;
        CardBorder.Opacity         = 1;
    }

    private async void OpenProfile()
    {
        if (CurrentTutorId == 0) return;
        var window = new ProfileViewWindow(CurrentTutorId);
        await window.ShowDialog(MainWindow.Instance);
    }
}