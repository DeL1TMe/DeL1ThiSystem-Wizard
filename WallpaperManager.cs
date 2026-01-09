using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard;

public static class WallpaperManager
{
    private const int SpiSetDeskWallpaper = 20;
    private const int SpifUpdateIniFile = 0x01;
    private const int SpifSendChange = 0x02;

    private const string DarkDesktop = @"C:\Wallpapers\dark_background_desktop.jpg";
    private const string LightDesktop = @"C:\Wallpapers\light_background_desktop.jpg";
    private const string DarkLock = @"C:\Wallpapers\dark_background_lockscreen.jpg";
    private const string LightLock = @"C:\Wallpapers\light_background_lockscreen.jpg";

    public static void ApplyTheme(string themeChoice)
    {
        bool light = string.Equals(themeChoice, "light", StringComparison.OrdinalIgnoreCase);
        var desktop = light ? LightDesktop : DarkDesktop;
        var lockscreen = light ? LightLock : DarkLock;

        SetDesktopWallpaper(desktop);
        SetLockScreen(lockscreen);
    }

    private static void SetDesktopWallpaper(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;

            using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop", true);
            key?.SetValue("Wallpaper", path, RegistryValueKind.String);
            key?.SetValue("WallpaperStyle", "10", RegistryValueKind.String);
            key?.SetValue("TileWallpaper", "0", RegistryValueKind.String);

            SystemParametersInfo(SpiSetDeskWallpaper, 0, path, SpifUpdateIniFile | SpifSendChange);
        }
        catch
        {
        }
    }

    private static void SetLockScreen(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;

            using var key = Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", true);
            key?.SetValue("LockScreenImagePath", path, RegistryValueKind.String);
            key?.SetValue("LockScreenImageStatus", 1, RegistryValueKind.DWord);
        }
        catch
        {
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SystemParametersInfo(int uiAction, int uiParam, string pvParam, int fWinIni);
}
