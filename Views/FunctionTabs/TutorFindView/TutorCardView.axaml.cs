using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MyApp.Models;

namespace MyApp.Views;

public partial class TutorCardView : UserControl
{
    public TutorCardView()
    {
        InitializeComponent();
        // Hiện placeholder khi chưa load data
        ShowPlaceholder();
    }

    public TutorCardView(TutorProfile tutor, double rating = 0, int reviewCount = 0)
    {
        InitializeComponent();
        LoadTutor(tutor, rating, reviewCount);
    }

    // ─── Load data vào card ───────────────────────────────────────────────────

    public void LoadTutor(TutorProfile tutor, double rating = 0, int reviewCount = 0)
    {
        TutorNameText.Text    = tutor.FullName;
        TutorTitleText.Text   = tutor.Profession;
        DegreeText.Text       = tutor.Degree;
        ExperienceText.Text   = tutor.Workplace;
        PriceText.Text        = $"{tutor.HourlyRate:N0}đ/giờ";
        DescriptionText.Text  = tutor.Description;
        RatingText.Text       = rating > 0 ? rating.ToString("F1") : "--";
        ReviewCountText.Text  = reviewCount > 0 ? $"{reviewCount} reviews" : "Chưa có đánh giá";

        ModeText.Text = tutor.TeachingMode switch
        {
            "online"  => "Online",
            "offline" => "Offline (dạy tại nhà)",
            "both"    => "Online & Offline",
            _         => "--"
        };

        // Load ảnh nếu có
        // TODO: Load avatar từ server
        // if (!string.IsNullOrEmpty(tutor.AvatarUrl))
        //     LoadImage(tutor.AvatarUrl);

        // Subjects
        LoadSubjectTags(tutor.Subjects);
    }

    private void LoadSubjectTags(List<Subject> subjects)
    {
        SubjectTagPanel.Children.Clear();
        foreach (var subject in subjects)
        {
            SubjectTagPanel.Children.Add(new Border
            {
                Background      = new SolidColorBrush(Color.Parse("#F0EDFF")),
                CornerRadius    = new Avalonia.CornerRadius(12),
                Padding         = new Avalonia.Thickness(10, 4),
                Margin          = new Avalonia.Thickness(0, 0, 6, 6),
                Child           = new TextBlock
                {
                    Text       = subject.Name,
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#7C6FCD")),
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                }
            });
        }
    }

    private void ShowPlaceholder()
    {
        TutorNameText.Text   = "Đang tải gia sư...";
        TutorTitleText.Text  = "";
        DegreeText.Text      = "--";
        ExperienceText.Text  = "--";
        PriceText.Text       = "--";
        ModeText.Text        = "--";
        RatingText.Text      = "--";
        ReviewCountText.Text = "--";
        DescriptionText.Text = "";
    }
}