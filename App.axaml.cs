using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MyApp.Data;
using MyApp.Views;
using MyApp.Services;

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

        Db = new Database("Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=123456");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        Localizer.Instance.ChangeLanguage("en");

        base.OnFrameworkInitializationCompleted();
    }
}