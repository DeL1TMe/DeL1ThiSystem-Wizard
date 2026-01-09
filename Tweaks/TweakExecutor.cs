using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Drawing;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static class TweakExecutor
{
    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DeL1ThiSystem",
        "Wizard");

    private static readonly string LogPath = Path.Combine(BaseDir, "ExeTweaks.log");

    private static readonly string[] ContentDeliveryValues =
    {
        "ContentDeliveryAllowed",
        "FeatureManagementEnabled",
        "OEMPreInstalledAppsEnabled",
        "PreInstalledAppsEnabled",
        "PreInstalledAppsEverEnabled",
        "SilentInstalledAppsEnabled",
        "SoftLandingEnabled",
        "SubscribedContentEnabled",
        "SubscribedContent-310093Enabled",
        "SubscribedContent-338387Enabled",
        "SubscribedContent-338388Enabled",
        "SubscribedContent-338389Enabled",
        "SubscribedContent-338393Enabled",
        "SubscribedContent-353694Enabled",
        "SubscribedContent-353696Enabled",
        "SubscribedContent-353698Enabled",
        "SystemPaneSuggestionsEnabled"
    };

    private static readonly string[] AppxSelectors =
    {
        "Microsoft.Microsoft3DViewer",
        "Microsoft.BingSearch",
        "Clipchamp.Clipchamp",
        "Microsoft.Copilot",
        "Microsoft.549981C3F5F10",
        "Microsoft.Windows.DevHome",
        "MicrosoftCorporationII.MicrosoftFamily",
        "Microsoft.WindowsFeedbackHub",
        "Microsoft.Edge.GameAssist",
        "Microsoft.GetHelp",
        "Microsoft.Getstarted",
        "Microsoft.BingNews",
        "Microsoft.MicrosoftOfficeHub",
        "Microsoft.Office.OneNote",
        "Microsoft.OutlookForWindows",
        "Microsoft.MSPaint",
        "Microsoft.People",
        "Microsoft.PowerAutomateDesktop",
        "MicrosoftCorporationII.QuickAssist",
        "Microsoft.SkypeApp",
        "Microsoft.MicrosoftSolitaireCollection",
        "Microsoft.MicrosoftStickyNotes",
        "MicrosoftTeams",
        "MSTeams",
        "Microsoft.Todos",
        "Microsoft.Wallet",
        "Microsoft.BingWeather",
        "Microsoft.Xbox.TCUI",
        "Microsoft.XboxApp",
        "Microsoft.XboxGameOverlay",
        "Microsoft.XboxGamingOverlay",
        "Microsoft.XboxIdentityProvider",
        "Microsoft.XboxSpeechToTextOverlay",
        "Microsoft.GamingApp",
        "Microsoft.ZuneVideo"
    };

    private static readonly string[] CapabilitySelectors =
    {
        "OneCoreUAP.OneSync",
        "App.Support.QuickAssist",
        "App.StepsRecorder"
    };

    private static readonly string[] FeatureSelectors =
    {
        "Recall"
    };

    public static void Execute(string id, string osFamily, string themeChoice)
    {
        EnsureLogDir();
        Log($"START {id}");
        try
        {
            switch (id)
            {
                case "bootstrap.defender_disable":
                    DisableDefenderNotifications();
                    break;
                case "bootstrap.smartscreen_disable":
                    DisableSmartScreen();
                    break;
                case "bootstrap.webcontent_eval_disable":
                    DisableWebContentEvaluation();
                    break;
                case "bootstrap.executionpolicy_remotesigned":
                    RunPowerShell("Set-ExecutionPolicy -Scope LocalMachine -ExecutionPolicy RemoteSigned -Force");
                    break;
                case "bootstrap.remote_access_enable":
                    EnableRemoteAssistance();
                    break;
                case "bootstrap.long_paths_enable":
                    SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", 1);
                    break;
                case "bootstrap.rdp_enable":
                    EnableRdp();
                    break;
                case "bootstrap.visualfx_profile":
                    ApplyVisualFxProfile();
                    break;
                case "bootstrap.sticky_keys_disable":
                    DisableStickyKeys();
                    break;
                case "bootstrap.enhance_pointer_precision_disable":
                    DisableEnhancePointerPrecision();
                    break;
                case "bootstrap.start_pins":
                    ConfigureStartPinsIfWin11();
                    break;
                case "apps.remove_uwp":
                    RemoveAppxPackages();
                    break;
                case "apps.remove_capabilities":
                    RemoveCapabilities();
                    break;
                case "apps.remove_features":
                    RemoveFeatures();
                    break;
                case "apps.onedrive_remove":
                    RemoveOneDriveArtifacts();
                    break;
                case "apps.edge_make_uninstallable":
                    MakeEdgeUninstallable();
                    break;
                case "apps.edge_background_disable":
                    SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge\Recommended", "BackgroundModeEnabled", 0);
                    break;
                case "apps.edge_startup_boost_disable":
                    SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge\Recommended", "StartupBoostEnabled", 0);
                    break;
                case "ui.color_theme":
                    ApplyWindowsTheme(themeChoice);
                    break;
                case "updates.pause_policy_task":
                    PauseWindowsUpdate();
                    break;
                case "updates.consumer_features_disable":
                    DisableConsumerFeatures();
                    break;
                case "updates.search_suggestions_disable":
                    SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
                    break;
                case "updates.widgets_disable":
                    DisableWidgetsAndNews();
                    break;
                case "perf.fast_startup_disable":
                    SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);
                    break;
                case "shell.classic_context_menu":
                    EnableClassicContextMenu();
                    break;
                case "shell.show_file_extensions":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0);
                    break;
                case "shell.hide_task_view":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0);
                    break;
                case "shell.search_box_mode":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1);
                    break;
                case "shell.taskbar_end_task":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1);
                    break;
                case "shell.tray_show_all_icons":
                    ShowAllTrayIcons();
                    break;
                case "extras.install_apps":
                case "extras.install_toolbox":
                case "extras.activate_hwid":
                    Log($"SKIP {id}: not implemented in exe.");
                    break;
                case "noop":
                    break;
                default:
                    Log($"WARN {id}: no handler.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR {id}: {ex}");
        }
        Log($"END {id}");
    }

    private static void EnsureLogDir()
    {
        try { Directory.CreateDirectory(BaseDir); } catch { }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
        }
        catch
        {
        }
    }

    private static void SetDword(RegistryHive hive, string subKey, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKey, true);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(RegistryHive hive, string subKey, string name, string value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKey, true);
        key?.SetValue(name, value, RegistryValueKind.String);
    }

    private static void SetDefaultValue(RegistryHive hive, string subKey, string value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKey, true);
        key?.SetValue(null, value, RegistryValueKind.String);
    }

    private static void DisableDefenderNotifications()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Notifications", "DisableNotifications", 1);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender Security Center\Systray", "HideSystray", 1);
    }

    private static void DisableSmartScreen()
    {
        SetString(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Off");
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "ServiceEnabled", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyMalicious", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyPasswordReuse", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyUnsafeApp", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Edge", "SmartScreenEnabled", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Edge", "SmartScreenPuaEnabled", 0);
    }

    private static void DisableWebContentEvaluation()
    {
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AppHost", "PreventOverride", 0);
    }

    private static void EnableRemoteAssistance()
    {
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 1);
    }

    private static void EnableRdp()
    {
        RunProcess("netsh.exe", "advfirewall firewall set rule group=\"@FirewallAPI.dll,-28752\" new enable=Yes");
        SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0);
    }

    private static void ApplyVisualFxProfile()
    {
        var key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
        SetDword(RegistryHive.LocalMachine, $@"{key}\ControlAnimations", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\AnimateMinMax", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\TaskbarAnimations", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DWMAeroPeekEnabled", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\MenuAnimation", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\TooltipAnimation", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\SelectionFade", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DWMSaveThumbnailEnabled", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\CursorShadow", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ListviewShadow", "DefaultValue", 1);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ThumbnailsOrIcon", "DefaultValue", 1);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ListviewAlphaSelect", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DragFullWindows", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ComboBoxAnimation", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\FontSmoothing", "DefaultValue", 1);
        SetDword(RegistryHive.LocalMachine, $@"{key}\ListBoxSmoothScrolling", "DefaultValue", 0);
        SetDword(RegistryHive.LocalMachine, $@"{key}\DropShadow", "DefaultValue", 1);
    }

    private static void DisableStickyKeys()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Accessibility\StickyKeys", "Flags", "10");
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(@".DEFAULT\Control Panel\Accessibility\StickyKeys", true);
        key?.SetValue("Flags", "10", RegistryValueKind.String);
    }

    private static void DisableEnhancePointerPrecision()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
    }

    private static void ConfigureStartPinsIfWin11()
    {
        if (Environment.OSVersion.Version.Build < 22000)
            return;

        const string json = "{\"pinnedList\":[]}";
        var key = @"SOFTWARE\Microsoft\PolicyManager\current\device\Start";
        SetString(RegistryHive.LocalMachine, key, "ConfigureStartPins", json);
        SetDword(RegistryHive.LocalMachine, key, "ConfigureStartPins_ProviderSet", 1);
        SetQword(RegistryHive.LocalMachine, key, "ConfigureStartPins_LastWrite", DateTime.UtcNow.ToFileTimeUtc());
        SetString(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\PolicyManager\default\device\Start", "ConfigureStartPins", json);
        RunProcess("cmd.exe", "/c taskkill /f /im explorer.exe & start explorer.exe");
    }

    private static void RemoveAppxPackages()
    {
        var selectors = string.Join(",", AppxSelectors.Select(s => $"'{s}'"));
        var script =
            "$selectors = @(" + selectors + ");" +
            "$prov = Get-AppxProvisionedPackage -Online;" +
            "foreach ($s in $selectors) { " +
            "  $prov | Where-Object { $_.DisplayName -eq $s } | ForEach-Object { " +
            "    Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -ErrorAction Continue | Out-Null; " +
            "  } " +
            "}" +
            "$installed = Get-AppxPackage -AllUsers;" +
            "$hasAllUsers = (Get-Command Remove-AppxPackage).Parameters.ContainsKey('AllUsers');" +
            "foreach ($s in $selectors) { " +
            "  $installed | Where-Object { $_.Name -like ($s + '*') -or $_.PackageFamilyName -like ($s + '*') } | ForEach-Object { " +
            "    if ($hasAllUsers) { Remove-AppxPackage -AllUsers -Package $_.PackageFullName -ErrorAction Continue } " +
            "    else { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Continue } " +
            "  } " +
            "}";
        RunPowerShell(script);
        DisableCortana();
    }

    private static void RemoveCapabilities()
    {
        var selectors = string.Join(",", CapabilitySelectors.Select(s => $"'{s}'"));
        var script =
            "$selectors = @(" + selectors + ");" +
            "Get-WindowsCapability -Online | " +
            "Where-Object { $_.State -notin @('NotPresent','Removed') -and $selectors -contains (($_.Name -split '~')[0]) } | " +
            "Remove-WindowsCapability -Online -ErrorAction Continue;";
        RunPowerShell(script);
    }

    private static void RemoveFeatures()
    {
        var selectors = string.Join(",", FeatureSelectors.Select(s => $"'{s}'"));
        var script =
            "$selectors = @(" + selectors + ");" +
            "Get-WindowsOptionalFeature -Online | " +
            "Where-Object { $_.State -notin @('Disabled','DisabledWithPayloadRemoved') -and $selectors -contains $_.FeatureName } | " +
            "Disable-WindowsOptionalFeature -Online -Remove -NoRestart -ErrorAction Continue;";
        RunPowerShell(script);
    }

    private static void RemoveOneDriveArtifacts()
    {
        TryDelete(@"C:\Users\Default\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\OneDrive.lnk");
        RunProcess("cmd.exe", "/c \"%SystemRoot%\\System32\\OneDriveSetup.exe\" /uninstall");
        RunProcess("cmd.exe", "/c \"%SystemRoot%\\SysWOW64\\OneDriveSetup.exe\" /uninstall");
        ForceDelete(@"C:\Windows\System32\OneDriveSetup.exe");
        ForceDelete(@"C:\Windows\SysWOW64\OneDriveSetup.exe");
    }

    private static void MakeEdgeUninstallable()
    {
        var path = @"C:\Windows\System32\IntegratedServicesRegionPolicySet.json";
        if (!File.Exists(path))
        {
            Log($"Edge policy missing: {path}");
            return;
        }

        try
        {
            var node = JsonNode.Parse(File.ReadAllText(path));
            var policies = node?["policies"]?.AsArray();
            if (policies == null)
                return;

            foreach (var item in policies)
            {
                if ((string?)item?["guid"] == "{1bca278a-5d11-4acf-ad2f-f9ab6d7f93a6}")
                    item!["defaultState"] = "enabled";
            }

            var json = node!.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Log($"Edge policy update failed: {ex.Message}");
        }
    }

    private static void PauseWindowsUpdate()
    {
        var now = DateTime.UtcNow;
        var start = now.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);
        var end = now.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);

        const string key = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        SetString(RegistryHive.LocalMachine, key, "PauseFeatureUpdatesStartTime", start);
        SetString(RegistryHive.LocalMachine, key, "PauseFeatureUpdatesEndTime", end);
        SetString(RegistryHive.LocalMachine, key, "PauseQualityUpdatesStartTime", start);
        SetString(RegistryHive.LocalMachine, key, "PauseQualityUpdatesEndTime", end);
        SetString(RegistryHive.LocalMachine, key, "PauseUpdatesStartTime", start);
        SetString(RegistryHive.LocalMachine, key, "PauseUpdatesExpiryTime", end);
    }

    private static void DisableConsumerFeatures()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
        foreach (var name in ContentDeliveryValues)
            SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", name, 0);
    }

    private static void EnableClassicContextMenu()
    {
        var key = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
        SetDefaultValue(RegistryHive.CurrentUser, key, "");
    }

    private static void DisableWidgetsAndNews()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowWidgets", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
    }

    private static void DisableCortana()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);
    }

    private static void ApplyWindowsTheme(string themeChoice)
    {
        bool light = string.Equals(themeChoice, "light", StringComparison.OrdinalIgnoreCase);
        int lightValue = light ? 1 : 0;

        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", lightValue);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", lightValue);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "ColorPrevalence", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1);

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

    private static void ShowAllTrayIcons()
    {
        if (Environment.OSVersion.Version.Build < 22000)
        {
            SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray", 0);
            return;
        }

        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using var root = baseKey.OpenSubKey(@"Control Panel\NotifyIconSettings", writable: true);
        if (root == null)
            return;

        foreach (var name in root.GetSubKeyNames())
        {
            using var sub = root.CreateSubKey(name, true);
            sub?.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
        }
    }

    private static void RunPowerShell(string command)
    {
        var bytes = Encoding.Unicode.GetBytes(command);
        var encoded = Convert.ToBase64String(bytes);
        RunProcess("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}");
    }

    private static void RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        using var proc = Process.Start(psi);
        proc?.WaitForExit();
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Log($"Delete failed: {path} ({ex.Message})");
        }
    }

    private static void ForceDelete(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;
            var cmd = $"takeown /f \"{path}\" /a & icacls \"{path}\" /grant Administrators:F /c & del /f /q \"{path}\"";
            RunProcess("cmd.exe", "/c " + cmd);
        }
        catch (Exception ex)
        {
            Log($"Force delete failed: {path} ({ex.Message})");
        }
    }

    private static void SetQword(RegistryHive hive, string subKey, string name, long value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKey, true);
        key?.SetValue(name, value, RegistryValueKind.QWord);
    }
}
