using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace MyApp.Views;

public partial class HomeView : UserControl
{
    private NavTab _currentTab = NavTab.Home;

    // Các tab view — lazy init, tạo 1 lần rồi giữ lại
    private UserControl? _discoverView;
    private UserControl? _messageView;
    private UserControl? _notifyView;
    private UserControl? _accountView;

    public HomeView()
    {
        InitializeComponent();
        SetActiveTab(NavTab.Home);
        LoadAvatar(); // ← thêm dòng này
    }

    private async void LoadAvatar()
    {
        if (Session.CurrentUserId == 0) return;

        var user = await new Data.Repositories.UserRepository(App.Db)
            .GetByIdAsync(Session.CurrentUserId);
        if (user == null) return;

        // Lấy tên hiển thị
        string displayName = user.Username;
        if (user.Role == "student")
        {
            var p = await new Data.Repositories.ProfileRepository(App.Db)
                .GetStudentByUserIdAsync(user.Id);
            if (p != null) displayName = p.DisplayName;
        }
        else
        {
            var p = await new Data.Repositories.ProfileRepository(App.Db)
                .GetTutorByUserIdAsync(user.Id);
            if (p != null) displayName = p.FullName;
        }

        // Hiện ảnh hoặc chữ cái đầu
        if (!string.IsNullOrEmpty(user.AvatarPath) &&
            System.IO.File.Exists(user.AvatarPath))
        {
            AvatarImage.Source      = new Avalonia.Media.Imaging.Bitmap(user.AvatarPath);
            AvatarBorder.IsVisible  = true;
            AvatarInitial.IsVisible = false;
        }
        else
        {
            AvatarInitial.Text      = displayName.Length > 0
                ? displayName[0].ToString().ToUpper() : "?";
            AvatarInitial.IsVisible = true;
            AvatarBorder.IsVisible  = false;
        }
    }

    // ─── Top bar ──────────────────────────────────────────────────────────────

    private void OnMenuClick(object? sender, RoutedEventArgs e)
    {
        MenuOverlay.IsVisible    = true;
        ProfileOverlay.IsVisible = false;
    }

    private void OnMenuOverlayPressed(object? sender, PointerPressedEventArgs e)
        => MenuOverlay.IsVisible = false;

    private void OnProfileClick(object? sender, RoutedEventArgs e)
    {
        ProfileOverlay.IsVisible = !ProfileOverlay.IsVisible;
        MenuOverlay.IsVisible    = false;
    }

    private void OnProfileOverlayPressed(object? sender, PointerPressedEventArgs e)
        => ProfileOverlay.IsVisible = false;

    private async void OnViewProfileClick(object? sender, RoutedEventArgs e)
    {
        ProfileOverlay.IsVisible = false;
        var window = new EditProfileWindow(Session.CurrentUserId);
        await window.ShowDialog(MainWindow.Instance);
    }

    private void OnLogoutClick(object? sender, RoutedEventArgs e)
    {
        ProfileOverlay.IsVisible = false;
        MainWindow.Instance.Navigate(new LoginView());
    }

    // ─── Bottom nav ───────────────────────────────────────────────────────────

    private void OnNavHomeClick(object? sender, RoutedEventArgs e)
        => SetActiveTab(NavTab.Home);

    private void OnNavMessageClick(object? sender, RoutedEventArgs e)
        => SetActiveTab(NavTab.Message);

    private void OnNavNotifyClick(object? sender, RoutedEventArgs e)
        => SetActiveTab(NavTab.Notify);

    private void OnNavAccountClick(object? sender, RoutedEventArgs e)
        => SetActiveTab(NavTab.Account);

    private void SetActiveTab(NavTab tab)
    {
        if (_currentTab == tab && BodyHost.Content != null) return;
        _currentTab = tab;

        // Đổi title
        TopBarTitle.Text = tab switch
        {
            NavTab.Home    => "Tìm Gia Sư",
            NavTab.Message => "Tin Nhắn",
            NavTab.Notify  => "Thông Báo",
            NavTab.Account => "Tài Khoản",
            _              => "Tìm Gia Sư",
        };

        // Swap body — lazy init
        BodyHost.Content = tab switch
        {
            NavTab.Home    => _discoverView ??= new DiscoverView(),
            NavTab.Message => _messageView  ??= new MessageView(),
            NavTab.Notify  => _notifyView   ??= new NotifyView(),
            NavTab.Account => _accountView  ??= new AccountView(),
            _              => _discoverView ??= new DiscoverView(),
        };

        // Update nav colors
        var active   = Color.Parse("#7C6FCD");
        var inactive = Color.Parse("#9090A8");

        SetNavColor(NavHomeIcon,  NavHomeText,  tab == NavTab.Home,    active, inactive);
        SetNavColor(NavMsgIcon,   NavMsgText,   tab == NavTab.Message, active, inactive);
        SetNavColor(NavNotiIcon,  NavNotiText,  tab == NavTab.Notify,  active, inactive);
        SetNavColor(NavAccIcon,   NavAccText,   tab == NavTab.Account, active, inactive);
    }

    // ─── Nav color helpers ────────────────────────────────────────────────────

    private static void SetNavColor(Polygon icon, TextBlock label,
                                    bool active, Color on, Color off)
    {
        var brush = new SolidColorBrush(active ? on : off);
        icon.Fill        = brush;
        label.Foreground = brush;
        label.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal;
    }

    private static void SetNavColor(Border icon, TextBlock label,
                                    bool active, Color on, Color off)
    {
        var brush = new SolidColorBrush(active ? on : off);
        icon.BorderBrush = brush;
        label.Foreground = brush;
        label.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal;
    }

    private static void SetNavColor(StackPanel icon, TextBlock label,
                                    bool active, Color on, Color off)
    {
        var brush = new SolidColorBrush(active ? on : off);
        label.Foreground = brush;
        label.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal;
        foreach (var child in icon.Children)
        {
            if (child is Ellipse e) e.Fill       = brush;
            if (child is Border  b) b.Background = brush;
        }
    }

    // ─── Public helpers ───────────────────────────────────────────────────────

    public void SetMessageBadge(int count)
    {
        MsgBadge.IsVisible = count > 0;
        MsgBadgeText.Text  = count > 99 ? "99+" : count.ToString();
    }

    public void SetNotifyBadge(int count)
    {
        NotiBadge.IsVisible = count > 0;
        NotiBadgeText.Text  = count > 99 ? "99+" : count.ToString();
    }

    public void SwitchToTab(NavTab tab) => SetActiveTab(tab);
}

public enum NavTab { Home, Message, Notify, Account }