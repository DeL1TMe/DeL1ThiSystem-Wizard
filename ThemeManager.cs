using System;
using System.Linq;
using System.Windows;

namespace DeL1ThiSystem.ConfigurationWizard;

public static class ThemeManager
{
    private static readonly Uri DarkUri = new("Themes/Theme.Dark.xaml", UriKind.Relative);
    private static readonly Uri LightUri = new("Themes/Theme.Light.xaml", UriKind.Relative);

    public static void ApplyTheme(string theme)
    {
        var app = Application.Current;
        if (app == null)
            return;

        var uri = string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ? LightUri : DarkUri;
        var dictionaries = app.Resources.MergedDictionaries;

        var existing = dictionaries
            .FirstOrDefault(d => d.Source != null && (d.Source.OriginalString == DarkUri.OriginalString || d.Source.OriginalString == LightUri.OriginalString));
        if (existing != null)
            dictionaries.Remove(existing);

        dictionaries.Insert(0, new ResourceDictionary { Source = uri });
    }
}
