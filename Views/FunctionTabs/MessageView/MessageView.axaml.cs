using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class MessageView : UserControl
{
    private readonly int _currentUserId;
    private List<Conversation> _conversations = new();

    public MessageView()
    {
        InitializeComponent();
        // TODO: Lấy userId từ session thực tế
        _currentUserId = Session.CurrentUserId;
        LoadConversations();
    }

    private async void LoadConversations()
    {
        var repo = new MessageRepository(App.Db);
        _conversations = (await repo.GetConversationsAsync(_currentUserId)).ToList();

        LoadingText.IsVisible = false;

        if (_conversations.Count == 0)
        {
            EmptyPanel.IsVisible = true;
            return;
        }

        ListPanel.IsVisible = true;
        ConvList.Children.Clear();

        foreach (var conv in _conversations)
            ConvList.Children.Add(BuildConvItem(conv));
    }

    private Button BuildConvItem(Conversation conv)
    {
        // Tên hiển thị — tuỳ role hiện tên người kia
        var otherName = "Người dùng";
        // TODO: Load tên từ DB theo student_id/tutor_id

        var btn = new Button { Classes = { "conv-item" } };
        btn.Tag    = conv.Id;
        btn.Click += OnConvClick;

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("52,*,Auto"),
            Margin            = new Avalonia.Thickness(16, 12),
        };

        // Avatar
        var avatar = new Border
        {
            Width        = 44,
            Height       = 44,
            CornerRadius = new Avalonia.CornerRadius(22),
            Background   = new SolidColorBrush(Color.Parse("#EEE8FF")),
            Child        = new TextBlock
            {
                Text                = "👤",
                FontSize            = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
            }
        };
        Grid.SetColumn(avatar, 0);
        grid.Children.Add(avatar);

        // Tên + preview
        var textStack = new StackPanel
        {
            Spacing           = 3,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Avalonia.Thickness(12, 0, 8, 0),
        };
        textStack.Children.Add(new TextBlock
        {
            Text       = otherName,
            FontSize   = 14,
            FontWeight = conv.UnreadCount > 0 ? FontWeight.Bold : FontWeight.Normal,
            Foreground = new SolidColorBrush(Color.Parse("#111122")),
        });
        textStack.Children.Add(new TextBlock
        {
            Text             = conv.LastMessage,
            FontSize         = 12,
            Foreground       = new SolidColorBrush(
                conv.UnreadCount > 0
                    ? Color.Parse("#333344")
                    : Color.Parse("#9090A8")),
            TextTrimming     = Avalonia.Media.TextTrimming.CharacterEllipsis,
            MaxWidth         = 180,
        });
        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        // Thời gian + badge
        var rightStack = new StackPanel
        {
            Spacing             = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        rightStack.Children.Add(new TextBlock
        {
            Text                = FormatTime(conv.LastMessageAt),
            FontSize            = 11,
            Foreground          = new SolidColorBrush(Color.Parse("#9090A8")),
            HorizontalAlignment = HorizontalAlignment.Right,
        });

        if (conv.UnreadCount > 0)
        {
            rightStack.Children.Add(new Border
            {
                Background          = new SolidColorBrush(Color.Parse("#7C6FCD")),
                CornerRadius        = new Avalonia.CornerRadius(10),
                MinWidth            = 20,
                Height              = 20,
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding             = new Avalonia.Thickness(5, 0),
                Child               = new TextBlock
                {
                    Text                = conv.UnreadCount > 99 ? "99+" : conv.UnreadCount.ToString(),
                    FontSize            = 10,
                    FontWeight          = FontWeight.Bold,
                    Foreground          = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                }
            });
        }
        Grid.SetColumn(rightStack, 2);
        grid.Children.Add(rightStack);

        // Divider
        var wrapper = new StackPanel();
        wrapper.Children.Add(grid);
        wrapper.Children.Add(new Border
        {
            Height     = 1,
            Background = new SolidColorBrush(Color.Parse("#F0F0F5")),
            Margin     = new Avalonia.Thickness(72, 0, 0, 0),
        });

        btn.Content = wrapper;
        return btn;
    }

    private void OnConvClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int convId) return;
        var conv = _conversations.FirstOrDefault(c => c.Id == convId);
        if (conv == null) return;

        MainWindow.Instance.Navigate(new ChatView(conv, _currentUserId));
    }

    private static string FormatTime(DateTime dt)
    {
        var now  = DateTime.UtcNow;
        var diff = now - dt;
        if (diff.TotalMinutes < 1)  return "Vừa xong";
        if (diff.TotalHours   < 1)  return $"{(int)diff.TotalMinutes}p";
        if (diff.TotalDays    < 1)  return $"{(int)diff.TotalHours}h";
        if (diff.TotalDays    < 7)  return $"{(int)diff.TotalDays}d";
        return dt.ToString("dd/MM");
    }
}