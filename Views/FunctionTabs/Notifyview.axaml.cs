using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class NotifyView : UserControl
{
    private readonly NotificationRepository _repo;

    public NotifyView()
    {
        _repo = new NotificationRepository(App.Db);
        InitializeComponent();
        LoadNotifications();
    }

    // ─── Load ─────────────────────────────────────────────────────────────────

    private async void LoadNotifications()
    {
        if (Session.CurrentUserId == 0) return;

        var items = (await _repo.GetByUserAsync(Session.CurrentUserId)).ToList();

        LoadingText.IsVisible = false;

        if (items.Count == 0)
        {
            EmptyPanel.IsVisible = true;
            return;
        }

        ListPanel.IsVisible = true;
        NotiList.Children.Clear();

        foreach (var n in items)
            NotiList.Children.Add(BuildItem(n));
    }

    // ─── Build item ───────────────────────────────────────────────────────────

    private Control BuildItem(Notification n)
    {
        var (icon, accent) = n.Type switch
        {
            "approval" => ("✓", "#4CAF50"),
            "interest" => ("❤", "#FC5C65"),
            "admin"    => ("📢", "#7C6FCD"),
            _          => ("🔔", "#9090A8"),
        };

        var btn   = new Button { Classes = { "noti-item" } };
        btn.Tag    = n.Id;
        btn.Click += OnItemClick;

        // ── Icon ──────────────────────────────────────────────────────────────
        var iconBorder = new Border
        {
            Width        = 36,
            Height       = 36,
            CornerRadius = new Avalonia.CornerRadius(18),
            Background   = new SolidColorBrush(Color.Parse(accent + "22")),
            Child        = new TextBlock
            {
                Text                = icon,
                FontSize            = 16,
                Foreground          = new SolidColorBrush(Color.Parse(accent)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            }
        };

        // ── Text ──────────────────────────────────────────────────────────────
        var titleText = new TextBlock
        {
            Text       = n.Title,
            FontSize   = 13,
            FontWeight = n.IsRead ? FontWeight.Normal : FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#111122")),
        };

        var bodyText = new TextBlock
        {
            Text         = n.Body,
            FontSize     = 12,
            Foreground   = new SolidColorBrush(Color.Parse("#9090A8")),
            TextWrapping = TextWrapping.Wrap,
        };

        var timeText = new TextBlock
        {
            Text       = FormatTime(n.CreatedAt),
            FontSize   = 11,
            Foreground = new SolidColorBrush(Color.Parse("#BBBBCC")),
            Margin     = new Avalonia.Thickness(0, 2, 0, 0),
        };

        var textStack = new StackPanel { Spacing = 3 };
        textStack.Children.Add(titleText);
        textStack.Children.Add(bodyText);
        textStack.Children.Add(timeText);

        // ── Unread dot ────────────────────────────────────────────────────────
        var dot = new Ellipse
        {
            Width               = 8,
            Height              = 8,
            Fill                = new SolidColorBrush(Color.Parse("#7C6FCD")),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Top,
            IsVisible           = !n.IsRead,
        };

        // ── Right side: text + dot ────────────────────────────────────────────
        var rightGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,12"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Avalonia.Thickness(10, 0, 0, 0),
        };
        Grid.SetColumn(textStack, 0);
        Grid.SetColumn(dot, 1);
        rightGrid.Children.Add(textStack);
        rightGrid.Children.Add(dot);

        // ── Row: icon + right ─────────────────────────────────────────────────
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("44,*"),
            Margin            = new Avalonia.Thickness(16, 12),
            Background        = n.IsRead
                ? Brushes.Transparent
                : new SolidColorBrush(Color.Parse("#F8F6FF")),
        };
        Grid.SetColumn(iconBorder, 0);
        Grid.SetColumn(rightGrid,  1);
        row.Children.Add(iconBorder);
        row.Children.Add(rightGrid);

        // ── Wrapper: row + divider ────────────────────────────────────────────
        var wrapper = new StackPanel { Spacing = 0 };
        wrapper.Children.Add(row);
        wrapper.Children.Add(new Border
        {
            Height     = 1,
            Background = new SolidColorBrush(Color.Parse("#F0F0F5")),
            Margin     = new Avalonia.Thickness(16, 0),
        });

        btn.Content = wrapper;
        return btn;
    }

    // ─── Events ───────────────────────────────────────────────────────────────

    private async void OnItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int id) return;
        await _repo.MarkReadAsync(id);
        LoadNotifications();
    }

    private async void OnMarkAllReadClick(object? sender, RoutedEventArgs e)
    {
        await _repo.MarkAllReadAsync(Session.CurrentUserId);
        LoadNotifications();
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static string FormatTime(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Vừa xong";
        if (diff.TotalHours   < 1) return $"{(int)diff.TotalMinutes} phút trước";
        if (diff.TotalDays    < 1) return $"{(int)diff.TotalHours} giờ trước";
        if (diff.TotalDays    < 7) return $"{(int)diff.TotalDays} ngày trước";
        return dt.ToLocalTime().ToString("dd/MM/yyyy");
    }
}