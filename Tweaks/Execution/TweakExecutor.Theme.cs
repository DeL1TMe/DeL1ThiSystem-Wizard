using System;
using System.Drawing;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    public static void ApplyWindowsTheme(string themeChoice)
    {
        bool light = string.Equals(themeChoice, "light", StringComparison.OrdinalIgnoreCase);
        int lightValue = light ? 1 : 0;

        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", lightValue);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", lightValue);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "ColorPrevalence", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);

        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", lightValue);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", lightValue);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "ColorPrevalence", 0);
        SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);

        const string htmlAccentColor = "#0078D4";
        var accentColor = ColorTranslator.FromHtml(htmlAccentColor);

        uint ConvertToDword(Color color)
        {
            byte[] bytes = { color.R, color.G, color.B, color.A };
            return BitConverter.ToUInt32(bytes, 0);
        }

        var startColor = Color.FromArgb(0xD2, accentColor);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "StartColorMenu", unchecked((int)ConvertToDword(startColor)));
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentColorMenu", unchecked((int)ConvertToDword(accentColor)));
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\DWM", "AccentColor", unchecked((int)ConvertToDword(accentColor)));

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", writable: true);
            var palette = key?.GetValue("AccentPalette") as byte[];
            if (palette != null && palette.Length >= 24)
            {
                int index = 20;
                palette[index++] = accentColor.R;
                palette[index++] = accentColor.G;
                palette[index++] = accentColor.B;
                palette[index++] = accentColor.A;
                key?.SetValue("AccentPalette", palette, RegistryValueKind.Binary);
            }
        }
        catch (Exception ex)
        {
            Log($"Accent palette update failed: {ex.Message}");
        }

        DeL1ThiSystem.ConfigurationWizard.WallpaperManager.ApplyTheme(themeChoice);
    }

}
