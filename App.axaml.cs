using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MyApp.Data;
using MyApp.Views;

namespace MyApp;

public partial class App : Application
{
    public static Database Db { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        Db = new Database("Host=10.179.101.18;Port=5433;Database=myapp;Username=postgres;Password=123456");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}