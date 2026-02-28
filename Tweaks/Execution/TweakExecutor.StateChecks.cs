using System;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{
    private static bool IsTweakAlreadyApplied(string id, string osFamily, string themeChoice)
    {
        return id switch
        {
            // ── Bootstrap ──────────────────────────────────────────
            "bootstrap.defender_disable" => IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications", "DisableNotifications", 1)
                                            && IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray", "HideSystray", 1),
            "bootstrap.smartscreen_disable" => IsStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off"),
            "bootstrap.webcontent_eval_disable" => IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0),
            "bootstrap.executionpolicy_remotesigned" => IsStringValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\PowerShell", "ExecutionPolicy", "RemoteSigned"),
            "bootstrap.remote_access_enable" => IsDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 1),
            "bootstrap.long_paths_enable" => IsDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1),
            "bootstrap.rdp_enable" => IsDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0),
            "bootstrap.sticky_keys_disable" => IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "10"),
            "bootstrap.restore_disable_cleanup" => false,
            "bootstrap.enhance_pointer_precision_disable" => IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0")
                                                            && IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0")
                                                            && IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0"),
            "bootstrap.wallpaper_quality_100" => IsDwordValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "JPEGImportQuality", 100),
            "bootstrap.configure_ru_ru_locale" => false,

            // ── Locale (internal steps) ────────────────────────────
            "system.configure_ru_ru_locale_utf8" => false,
            "system.cleanup_ru_ru_local_packages" => false,

            // ── Profile ────────────────────────────────────────────
            "profile.sticky_keys_disable" => IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "10"),
            "profile.enhance_pointer_precision_disable" => IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0")
                                                           && IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0")
                                                           && IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0"),
            "profile.wallpaper_quality_100" => IsDwordValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "JPEGImportQuality", 100),
            "profile.configure_ru_ru_user_locale" => false,

            // ── Apps ───────────────────────────────────────────────
            "apps.edge_restrict" => IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "UninstallAllowed", 1)
                                    && IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge\Recommended", "BackgroundModeEnabled", 0)
                                    && IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge\Recommended", "StartupBoostEnabled", 0)
                                    && IsEdgeDesktopShortcutRemoved(),
            "apps.remove_uwp" => false,
            "apps.remove_components" => false,
            "apps.onedrive_remove" => !File.Exists(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\OneDriveSetup.exe"))
                                      && !File.Exists(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\SysWOW64\OneDriveSetup.exe")),

            // ── Privacy ────────────────────────────────────────────
            "privacy.disable_tracking" => IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1)
                                          && IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1)
                                          && IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowWidgets", 0)
                                          && IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
            "privacy.pause_updates" => IsPauseConfigured(),

            // ── Performance ────────────────────────────────────────
            "perf.fast_startup_disable" => IsDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0),
            "perf.powercfg_never_sleep" => false,
            "perf.visualfx_profile" => IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 3)
                                       && IsStringValue(RegistryHive.CurrentUser, @"Control Panel\Desktop", "DragFullWindows", "0"),
            "perf.memory_integrity_disable" => IsDwordValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", 0),

            // ── Shell ──────────────────────────────────────────────
            "shell.taskbar_cleanup" => IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0)
                                      && IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1),
            "shell.start_menu_cleanup" => false, // always re-run; tiles/pins are hard to detect
            "shell.explorer_settings" => IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0)
                                         && IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1),
            "shell.desktop_icons_minimal" => IsDesktopIconsMinimalApplied(),
            "shell.classic_context_menu" => IsStringValue(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", null, string.Empty),

            // ── Theme ──────────────────────────────────────────────
            "ui.color_theme" => IsThemeApplied(themeChoice),

            // ── Extras ─────────────────────────────────────────────
            "extras.install_apps" => false,
            "extras.install_toolbox" => false,
            "extras.activate_hwid" => File.Exists(Path.Combine(BaseDir, "hwid_activated.marker")),

            "noop" => true,
            _ => false
        };
    }

    private static bool IsThemeApplied(string themeChoice)
    {
        var light = string.Equals(themeChoice, "light", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        return IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", light)
               && IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", light);
    }

    private static bool IsPauseConfigured()
    {
        if (!TryGetString(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "PauseUpdatesExpiryTime", out var value))
            return false;
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return false;
        return dt.ToUniversalTime() > DateTime.UtcNow.AddMinutes(5);
    }

    private static bool IsTrayShowAllApplied()
    {
        if (Environment.OSVersion.Version.Build < 22000)
            return IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray", 0);

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var root = baseKey.OpenSubKey(@"Control Panel\NotifyIconSettings", false);
            if (root == null)
                return false;
            foreach (var name in root.GetSubKeyNames())
            {
                using var sub = root.OpenSubKey(name, false);
                var value = sub?.GetValue("IsPromoted");
                if (value is int i && i == 1)
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool IsDesktopIconsMinimalApplied()
    {
        return IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", "{20d04fe0-3aea-1069-a2d8-08002b30309d}", 0)
               && IsDwordValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20d04fe0-3aea-1069-a2d8-08002b30309d}", 0);
    }

    private static bool IsEdgeDesktopShortcutRemoved()
    {
        if (!IsDwordValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "RemoveDesktopShortcutDefault", 1))
            return false;

        static bool hasEdgeLinks(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return false;
                foreach (var link in Directory.GetFiles(path, "*.lnk"))
                {
                    var name = Path.GetFileNameWithoutExtension(link);
                    if (name.IndexOf("edge", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        var user = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var common = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        var def = @"C:\Users\Default\Desktop";
        return !hasEdgeLinks(user) && !hasEdgeLinks(common) && !hasEdgeLinks(def);
    }

    private static bool IsDwordValue(RegistryHive hive, string subKey, string name, int expected)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey, false);
            var value = key?.GetValue(name);
            return value is int i && i == expected;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsStringValue(RegistryHive hive, string subKey, string? name, string expected)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey, false);
            var value = key?.GetValue(name);
            var str = value?.ToString() ?? string.Empty;
            return string.Equals(str, expected, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetString(RegistryHive hive, string subKey, string name, out string value)
    {
        value = string.Empty;
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey, false);
            var raw = key?.GetValue(name);
            if (raw == null)
                return false;
            value = raw.ToString() ?? string.Empty;
            return value.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
