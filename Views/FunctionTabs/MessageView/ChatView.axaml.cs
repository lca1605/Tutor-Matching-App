using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using MyApp.Data.Repositories;
using MyApp.Models;

namespace MyApp.Views;

public partial class ChatView : UserControl
{
    private readonly Conversation      _conversation;
    private readonly int               _currentUserId;
    private readonly MessageRepository _repo;
    private const    int               PageSize = 50;

    // Bắt buộc cho XAML compiler, không dùng trực tiếp
    public ChatView()
    {
        _conversation  = new Conversation();
        _currentUserId = 0;
        _repo          = new MessageRepository(App.Db);
        InitializeComponent();
    }

    public ChatView(Conversation conversation, int currentUserId)
    {
        _conversation  = conversation;
        _currentUserId = currentUserId;
        _repo          = new MessageRepository(App.Db);

        InitializeComponent();

        OtherNameText.Text = "Người dùng"; // TODO: load tên thật từ DB

        MessageInput.TextChanged += (_, _) =>
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageInput.Text);

        MessageInput.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && SendButton.IsEnabled)
                OnSendClick(null, null!);
        };

        LoadMessages();
    }

    // ─── Load từ DB ───────────────────────────────────────────────────────────

    private async void LoadMessages()
    {
        if (_conversation.Id == 0) return;

        var messages = (await _repo.GetMessagesAsync(_conversation.Id, PageSize, 0))
            .ToList();

        await _repo.MarkAsReadAsync(_conversation.Id, _currentUserId);

        messages.Reverse(); // DB trả mới nhất trước, đảo lại cho đúng thứ tự

        MessageList.Children.Clear();
        foreach (var msg in messages)
            MessageList.Children.Add(BuildMessageBubble(msg));

        ScrollToBottom();
    }

    // ─── Build bubble ─────────────────────────────────────────────────────────

    private Control BuildMessageBubble(Message msg)
    {
        System.Console.WriteLine($"SenderId={msg.SenderId} CurrentUserId={_currentUserId} isMine={msg.SenderId == _currentUserId}");

        var isMine      = msg.SenderId == _currentUserId;
        var bubbleColor = isMine ? "#7C6FCD" : "#FFFFFF";
        var textColor   = isMine ? "#FFFFFF"  : "#111122";
        var align       = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        Control content;

        if (msg.MessageType == "image" && msg.FilePath != null)
        {
            content = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(12),
                ClipToBounds = true,
                MaxWidth     = 220,
                Child        = new Image
                {
                    Source  = new Avalonia.Media.Imaging.Bitmap(msg.FilePath),
                    Stretch = Stretch.UniformToFill,
                }
            };
        }
        else if (msg.MessageType == "file")
        {
            var fileBtn = new Button
            {
                Background      = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding         = new Avalonia.Thickness(0),
                Cursor          = new Cursor(StandardCursorType.Hand),
                Tag             = msg.FilePath,
            };
            fileBtn.Click  += OnOpenFileClick;
            fileBtn.Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing     = 8,
                Children    =
                {
                    new TextBlock { Text = "📎", FontSize = 18,
                                    VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock { Text = msg.FileName ?? "File", FontSize = 13,
                                    Foreground      = new SolidColorBrush(Color.Parse(textColor)),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    TextDecorations = isMine ? null : TextDecorations.Underline }
                }
            };
            content = fileBtn;
        }
        else
        {
            content = new TextBlock
            {
                Text         = msg.Content,
                FontSize     = 14,
                Foreground   = new SolidColorBrush(Color.Parse(textColor)),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth     = 240,
            };
        }

        var bubble = new Border
        {
            Background      = new SolidColorBrush(Color.Parse(bubbleColor)),
            CornerRadius    = isMine
                ? new Avalonia.CornerRadius(16, 4, 16, 16)
                : new Avalonia.CornerRadius(4, 16, 16, 16),
            Padding         = new Avalonia.Thickness(12, 8),
            MaxWidth        = 260,
            BorderBrush     = isMine ? null : new SolidColorBrush(Color.Parse("#EBEBF0")),
            BorderThickness = isMine ? new Avalonia.Thickness(0) : new Avalonia.Thickness(1),
            Child           = content,
        };

        var timeText = new TextBlock
        {
            Text                = msg.CreatedAt.ToLocalTime().ToString("HH:mm"),
            FontSize            = 10,
            Foreground          = new SolidColorBrush(Color.Parse("#9090A8")),
            HorizontalAlignment = align,
            Margin              = new Avalonia.Thickness(4, 2, 4, 0),
        };

        var row = new StackPanel
        {
            HorizontalAlignment = align,
            Spacing             = 2,
            Margin              = new Avalonia.Thickness(0, 2),
        };
        row.Children.Add(bubble);
        row.Children.Add(timeText);
        return row;
    }

    // ─── Gửi text ─────────────────────────────────────────────────────────────

    private async void OnSendClick(object? sender, RoutedEventArgs e)
    {
        var text = MessageInput.Text?.Trim();
        if (string.IsNullOrEmpty(text) || _conversation.Id == 0) return;

        MessageInput.Text    = "";
        SendButton.IsEnabled = false;

        await _repo.SendTextAsync(_conversation.Id, _currentUserId, text);

        MessageList.Children.Add(BuildMessageBubble(new Message
        {
            ConversationId = _conversation.Id,
            SenderId       = _currentUserId,
            Content        = text,
            MessageType    = "text",
            CreatedAt      = DateTime.UtcNow,
        }));
        ScrollToBottom();
    }

    // ─── Gửi file ─────────────────────────────────────────────────────────────

    private async void OnAttachClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "Chọn file", AllowMultiple = false });

        if (files.Count == 0) return;

        await _repo.SendFileAsync(_conversation.Id, _currentUserId,
                                  files[0].Path.LocalPath);
        LoadMessages();
    }

    // ─── Mở file ──────────────────────────────────────────────────────────────

    private void OnOpenFileClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path) return;
        if (!System.IO.File.Exists(path)) return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = path,
            UseShellExecute = true,
        });
    }

    // ─── Back ─────────────────────────────────────────────────────────────────

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        var home = new HomeView();
        MainWindow.Instance.Navigate(home);
        home.SwitchToTab(NavTab.Message);
    }

    // ─── Scroll ───────────────────────────────────────────────────────────────

    private void ScrollToBottom()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => MessagesScroll.ScrollToEnd(),
            Avalonia.Threading.DispatcherPriority.Background);
    }
}