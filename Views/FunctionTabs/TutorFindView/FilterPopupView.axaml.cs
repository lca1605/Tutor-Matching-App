using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MyApp.Views;

// ── Filter options được truyền ra ngoài khi apply ────────────────────────────
public class FilterOptions
{
    public string? SubjectName { get; set; }
    public decimal? MinPrice   { get; set; }
    public decimal? MaxPrice   { get; set; }
    public int MinRating       { get; set; } = 0;

    /// <summary>"all" | "online" | "offline"</summary>
    public string TeachingMode { get; set; } = "all";
}

public partial class FilterPopupView : UserControl
{
    private readonly Action<FilterOptions> _onApply;
    private readonly Action _onClose;

    private int    _selectedStar = 0;
    private string _selectedMode = "all";

    // Môn học mẫu — thực tế load từ DB
    private static readonly List<string> SubjectItems = new()
    {
        "Toán", "Vật lý", "Hóa học", "Sinh học",
        "Ngữ văn", "Tiếng Anh", "Lập trình", "Âm nhạc",
    };

    public FilterPopupView()
    {
        _onApply = _ => { };
        _onClose = () => { };
        InitializeComponent();
        LoadSubjects();
    }

    public FilterPopupView(Action<FilterOptions> onApply, Action onClose)
    {
        _onApply = onApply;
        _onClose = onClose;
        InitializeComponent();
        LoadSubjects();
    }

    private void LoadSubjects()
    {
        // TODO: Load từ DB thực tế
        SubjectCombo.ItemsSource = SubjectItems;
    }

    // ─── Star rating ──────────────────────────────────────────────────────────

    private void OnStarClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _selectedStar = int.Parse(btn.Tag?.ToString() ?? "0");

        SetToggle(Star0Btn, _selectedStar == 0);
        SetToggle(Star3Btn, _selectedStar == 3);
        SetToggle(Star4Btn, _selectedStar == 4);
        SetToggle(Star5Btn, _selectedStar == 5);
    }

    // ─── Teaching mode ────────────────────────────────────────────────────────

    private void OnModeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _selectedMode = btn.Tag?.ToString() ?? "all";

        SetToggle(ModeAllBtn,     _selectedMode == "all");
        SetToggle(ModeOnlineBtn,  _selectedMode == "online");
        SetToggle(ModeOfflineBtn, _selectedMode == "offline");
    }

    // ─── Reset ────────────────────────────────────────────────────────────────

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        SubjectCombo.SelectedItem = null;
        MinPriceBox.Text          = "";
        MaxPriceBox.Text          = "";
        _selectedStar             = 0;
        _selectedMode             = "all";

        SetToggle(Star0Btn, true);
        SetToggle(Star3Btn, false);
        SetToggle(Star4Btn, false);
        SetToggle(Star5Btn, false);

        SetToggle(ModeAllBtn,     true);
        SetToggle(ModeOnlineBtn,  false);
        SetToggle(ModeOfflineBtn, false);
    }

    // ─── Apply ────────────────────────────────────────────────────────────────

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        decimal.TryParse(MinPriceBox.Text, out var min);
        decimal.TryParse(MaxPriceBox.Text, out var max);

        _onApply(new FilterOptions
        {
            SubjectName  = SubjectCombo.SelectedItem as string,
            MinPrice     = min > 0 ? min : null,
            MaxPrice     = max > 0 ? max : null,
            MinRating    = _selectedStar,
            TeachingMode = _selectedMode,
        });
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static void SetToggle(Button btn, bool active)
    {
        btn.Classes.Remove("mode-on");
        btn.Classes.Remove("mode-off");
        btn.Classes.Add(active ? "mode-on" : "mode-off");
    }
}