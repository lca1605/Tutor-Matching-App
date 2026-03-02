using System.Linq;
using Avalonia.Controls;
using MyApp.Data.Repositories;
using MyApp.Views;

namespace MyApp;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();

#if DEBUG
        InitDebug();
#else
        Navigate(new LoginView());
#endif
    }

    private async void InitDebug()
    {
        // Tạo data test thật trong DB
        await TestDataSeeder.SeedAsync();

        // Lấy conversation đầu tiên của test_student
        var repo  = new MessageRepository(App.Db);
        var convs = (await repo.GetConversationsAsync(Session.CurrentUserId)).ToList();

        if (convs.Count > 0)
            Navigate(new ChatView(convs.First(), Session.CurrentUserId));
        else
            Navigate(new HomeView());
    }

    public void Navigate(UserControl page)
    {
        PageHost.Content = page;
    }
}