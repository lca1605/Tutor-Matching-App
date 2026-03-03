using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MyApp.Services;

public class Localizer : INotifyPropertyChanged
{
    private static readonly ResourceManager _rm =
        new("MyApp.Resources.Strings", typeof(Localizer).Assembly);

    private static CultureInfo _culture = CultureInfo.CurrentUICulture;

    public static Localizer Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key]
        => _rm.GetString(key, _culture) ?? key;

    public void ChangeLanguage(string cultureCode)
    {
        _culture = new CultureInfo(cultureCode);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}