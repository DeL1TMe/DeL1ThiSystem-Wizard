using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
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
    private static readonly string InternetWaitMarker = Path.Combine(BaseDir, "waiting_internet.marker");

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
        "Microsoft.ZuneVideo",
        "Microsoft.WindowsStore",
        "Microsoft.StorePurchaseApp"
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
                case "bootstrap.wallpaper_quality_100":
                    SetWallpaperQuality100();
                    break;
                case "bootstrap.start_pins":
                    ConfigureStartPinsForOs(osFamily);
                    break;
                case "bootstrap.taskbar_only_explorer":
                    SetTaskbarOnlyExplorer();
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
                    DisableConsumerFeatures(osFamily);
                    break;
                case "updates.search_suggestions_disable":
                    SetDword(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
                    SetDefaultUserDword(@"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
                    break;
                case "updates.widgets_disable":
                    DisableWidgetsAndNews();
                    break;
                case "perf.fast_startup_disable":
                    SetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);
                    break;
                case "perf.powercfg_never_sleep":
                    SetPowercfgNeverSleep();
                    break;
                case "perf.restore_disable_cleanup":
                case "bootstrap.restore_disable_cleanup":
                    DisableRestoreAndCleanup();
                    break;
                case "shell.classic_context_menu":
                    EnableClassicContextMenu();
                    break;
                case "shell.show_file_extensions":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0);
                    break;
                case "shell.hide_task_view":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0);
                    break;
                case "shell.search_box_mode":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1);
                    break;
                case "shell.taskbar_only_explorer":
                    if (osFamily == "10")
                        SetTaskbarOnlyExplorer();
                    break;
                case "shell.taskbar_end_task":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings", "TaskbarEndTask", 1);
                    break;
                case "shell.tray_show_all_icons":
                    ShowAllTrayIcons();
                    break;
                case "shell.remove_edge_desktop_shortcut":
                    RemoveEdgeDesktopShortcut();
                    break;
                case "shell.win11_start_recommended_disable":
                    ConfigureWin11StartAndRecents();
                    break;
                case "extras.install_apps":
                    InstallAppsFromFolder();
                    break;
                case "extras.install_toolbox":
                    InstallToolbox();
                    break;
                case "extras.activate_hwid":
                    ActivateHwid();
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
        SetDefaultUserString(@"Control Panel\Accessibility\StickyKeys", "Flags", "10");
    }

    private static void DisableEnhancePointerPrecision()
    {
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0");
        SetString(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0");
        WithDefaultUserHive(root =>
        {
            using var mouse = root.CreateSubKey(@"Control Panel\Mouse", true);
            if (mouse == null)
                return;
            mouse.SetValue("MouseSpeed", "0", RegistryValueKind.String);
            mouse.SetValue("MouseThreshold1", "0", RegistryValueKind.String);
            mouse.SetValue("MouseThreshold2", "0", RegistryValueKind.String);
        });
    }

    private static void SetWallpaperQuality100()
    {
        SetDword(RegistryHive.CurrentUser, @"Control Panel\Desktop", "JPEGImportQuality", 100);
        SetDefaultUserDword(@"Control Panel\Desktop", "JPEGImportQuality", 100);
    }

    private static void ConfigureStartPinsForOs(string osFamily)
    {
        if (string.Equals(osFamily, "10", StringComparison.OrdinalIgnoreCase))
        {
            ClearStartPinsWin10();
            return;
        }

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
            "}" +
            "$yandex = Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -match 'Yandex' };" +
            "foreach ($p in $yandex) { " +
            "  Remove-AppxProvisionedPackage -Online -PackageName $p.PackageName -ErrorAction Continue | Out-Null; " +
            "}" +
            "$yandexInstalled = Get-AppxPackage -AllUsers | Where-Object { $_.Name -match 'Yandex' -or $_.PackageFamilyName -match 'Yandex' };" +
            "foreach ($p in $yandexInstalled) { " +
            "  if ($hasAllUsers) { Remove-AppxPackage -AllUsers -Package $p.PackageFullName -ErrorAction Continue } " +
            "  else { Remove-AppxPackage -Package $p.PackageFullName -ErrorAction Continue } " +
            "}";
        RunPowerShell(script);
        RemoveWindowsStore();
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

    private static void DisableConsumerFeatures(string osFamily)
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
        if (osFamily == "10")
        {
            SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableSoftLanding", 1);
            SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableThirdPartySuggestions", 1);
        }
        foreach (var name in ContentDeliveryValues)
            SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", name, 0);
        ApplyDefaultUserContentDelivery();
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

    private static void SetTaskbarOnlyExplorer()
    {
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0);
        ApplyDefaultUserDesktopIcons();
        var script = @"
$taskbar = Join-Path $env:APPDATA 'Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar';
if (Test-Path $taskbar) {
  Get-ChildItem $taskbar -Filter *.lnk -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue;
  $wsh = New-Object -ComObject WScript.Shell;
  $lnk = $wsh.CreateShortcut((Join-Path $taskbar 'File Explorer.lnk'));
  $lnk.TargetPath = ""$env:WINDIR\explorer.exe"";
  $lnk.Save();
}
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband' -Name Favorites,FavoritesResolve,FavoritesChanges -ErrorAction SilentlyContinue;
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue;
";
        RunPowerShell(script);
    }

    private static void ClearStartPinsWin10()
    {
        var script = @"
$layout = @'
<LayoutModificationTemplate xmlns=""http://schemas.microsoft.com/Start/2014/LayoutModification"" Version=""1"">
  <LayoutOptions StartTileGroupCellWidth=""6"" />
  <DefaultLayoutOverride>
    <StartLayoutCollection>
      <StartLayout GroupCellWidth=""6"" />
    </StartLayoutCollection>
  </DefaultLayoutOverride>
</LayoutModificationTemplate>
'@
$layoutPath = Join-Path $env:ProgramData 'DeL1ThiSystem\Wizard\StartLayout.xml';
$layout | Set-Content -LiteralPath $layoutPath -Encoding UTF8;
$policyPaths = @(
  'HKCU:\Software\Policies\Microsoft\Windows\Explorer',
  'HKLM:\Software\Policies\Microsoft\Windows\Explorer'
);
foreach ($p in $policyPaths) {
  New-Item -Path $p -Force | Out-Null;
  Set-ItemProperty -Path $p -Name StartLayoutFile -Value $layoutPath -Type String;
  Set-ItemProperty -Path $p -Name LockedStartLayout -Value 1 -Type DWord;
}
$paths = @(
  'HKCU:\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount',
  'HKCU:\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount'
);
foreach ($path in $paths) {
  if (Test-Path $path) {
    Get-ChildItem $path -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'Start' } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue;
  }
}
$tileDb = Join-Path $env:LOCALAPPDATA 'TileDataLayer\Database';
if (Test-Path $tileDb) { Remove-Item $tileDb -Recurse -Force -ErrorAction SilentlyContinue; }
$layouts = @(
  (Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Shell\DefaultLayouts.xml'),
  (Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Shell\LayoutModification.xml')
);
foreach ($l in $layouts) { if (Test-Path $l) { Remove-Item $l -Force -ErrorAction SilentlyContinue; } }
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue;
Stop-Process -Name StartMenuExperienceHost -Force -ErrorAction SilentlyContinue;
Start-Sleep -Seconds 2;
foreach ($p in $policyPaths) {
  Set-ItemProperty -Path $p -Name LockedStartLayout -Value 0 -Type DWord;
}
";
        RunPowerShell(script);
    }

    private static void RemoveWindowsStore()
    {
        var script = @"
$names = @('Microsoft.WindowsStore','Microsoft.StorePurchaseApp');
$prov = Get-AppxProvisionedPackage -Online;
foreach ($n in $names) {
  $prov | Where-Object { $_.DisplayName -eq $n } | ForEach-Object {
    Remove-AppxProvisionedPackage -Online -PackageName $_.PackageName -ErrorAction Continue | Out-Null;
  }
}
$installed = Get-AppxPackage -AllUsers;
$hasAllUsers = (Get-Command Remove-AppxPackage).Parameters.ContainsKey('AllUsers');
foreach ($n in $names) {
  $installed | Where-Object { $_.Name -like ($n + '*') -or $_.PackageFamilyName -like ($n + '*') } | ForEach-Object {
    if ($hasAllUsers) { Remove-AppxPackage -AllUsers -Package $_.PackageFullName -ErrorAction Continue }
    else { Remove-AppxPackage -Package $_.PackageFullName -ErrorAction Continue }
  }
}
";
        RunPowerShell(script);
    }

    private static void DisableCortana()
    {
        SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCortanaButton", 0);
    }

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

    private static int RunProcessWithTimeout(string fileName, string arguments, int timeoutMs, out bool timedOut)
    {
        timedOut = false;
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        using var proc = Process.Start(psi);
        if (proc == null)
            return -1;
        if (!proc.WaitForExit(timeoutMs))
        {
            timedOut = true;
            try { proc.Kill(true); } catch { }
            return -1;
        }
        return proc.ExitCode;
    }

    private static void InstallAppsFromFolder()
    {
        const string appsPath = @"C:\Apps";
        var logPath = Path.Combine(BaseDir, "Install-Apps.log");

        void LogApp(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
            }
            catch
            {
            }
        }

        try { Directory.CreateDirectory(BaseDir); } catch { }

        if (!Directory.Exists(appsPath))
        {
            LogApp($"Apps folder not found: {appsPath}");
            return;
        }

        if (IsInternetAvailable())
        {
            LogApp("Internet OK. Installing 7-Zip (if missing) and downloading RustDesk.");
            InstallSevenZipIfMissing();
            DownloadRustDesk();
        }
        else
        {
            LogApp("Internet not available. Skip 7-Zip and RustDesk download.");
        }

        var mapPath = Path.Combine(appsPath, "install-args.json");
        var argMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(mapPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(mapPath));
                foreach (var prop in doc.RootElement.EnumerateObject())
                    argMap[prop.Name] = prop.Value.GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                LogApp($"Failed to read install-args.json: {ex.Message}");
            }
        }

        var files = Directory.GetFiles(appsPath)
            .Select(path => new FileInfo(path))
            .Where(f => f.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                        || f.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Name.Equals("install-apps.ps1", StringComparison.OrdinalIgnoreCase)
                        && !f.Name.Equals("install-args.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name)
            .ToList();

        foreach (var f in files)
        {
            var name = f.Name;
            var ext = f.Extension.ToLowerInvariant();
            var args = argMap.TryGetValue(name, out var custom) ? custom : string.Empty;

            string fileName;
            string finalArgs;

            if (ext == ".msi")
            {
                fileName = "msiexec.exe";
                if (string.IsNullOrWhiteSpace(args))
                    finalArgs = $"/i \"{f.FullName}\" /qn /norestart";
                else if (args.TrimStart().StartsWith("/i", StringComparison.OrdinalIgnoreCase))
                    finalArgs = args;
                else
                    finalArgs = $"/i \"{f.FullName}\" {args}";
            }
            else
            {
                fileName = f.FullName;
                finalArgs = string.IsNullOrWhiteSpace(args) ? "/S" : args;
            }

            try
            {
                LogApp($"Running: {name} | Args: {finalArgs}");
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = finalArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit();
                LogApp($"ExitCode ({name}): {proc?.ExitCode}");
            }
            catch (Exception ex)
            {
                LogApp($"ERROR running {name}: {ex}");
            }
        }

        LogApp("=== Install-Apps finished ===");
    }

    private static void SetPowercfgNeverSleep()
    {
        RunProcess("powercfg.exe", "-change -standby-timeout-ac 0");
        RunProcess("powercfg.exe", "-change -monitor-timeout-ac 0");
        RunProcess("powercfg.exe", "-change -hibernate-timeout-ac 0");
        RunProcess("powercfg.exe", "-change -standby-timeout-dc 0");
        RunProcess("powercfg.exe", "-change -monitor-timeout-dc 0");
        RunProcess("powercfg.exe", "-change -hibernate-timeout-dc 0");
    }

    private static void DisableRestoreAndCleanup()
    {
        RunPowerShell("try { Disable-ComputerRestore -Drive 'C:\\\\' } catch {}");
        RunProcess("cmd.exe", "/c rmdir /s /q C:\\Windows.old");
    }

    private static void ApplyDefaultUserContentDelivery()
    {
        WithDefaultUserHive(root =>
        {
            using var key = root.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", true);
            if (key == null)
                return;
            foreach (var name in ContentDeliveryValues)
                key.SetValue(name, 0, RegistryValueKind.DWord);
        });
    }

    private static void ApplyDefaultUserDesktopIcons()
    {
        string[] hideIcons =
        {
            "{5399e694-6ce5-4d6c-8fce-1d8870fdcba0}",
            "{b4bfcc3a-db2c-424c-b029-7fe99a87c641}",
            "{a8cdff1c-4878-43be-b5fd-f8091c1c60d0}",
            "{374de290-123f-4565-9164-39c4925e467b}",
            "{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}",
            "{f874310e-b6b7-47dc-bc84-b9e6b38f5903}",
            "{1cf1260c-4dd0-4ebb-811f-33c572699fde}",
            "{f02c1a0d-be21-4350-88b0-7367fc96ef3c}",
            "{3add1653-eb32-4cb0-bbd7-dfa0abb5acca}",
            "{59031a47-3f72-44a7-89c5-5595fe6b30ee}",
            "{a0953c92-50dc-43bf-be83-3742fed03c9c}"
        };

        string[] showIcons =
        {
            "{645ff040-5081-101b-9f08-00aa002f954e}",
            "{20d04fe0-3aea-1069-a2d8-08002b30309d}"
        };

        void Apply(RegistryKey root, string subKey)
        {
            using var key = root.CreateSubKey(subKey, true);
            if (key == null)
                return;
            foreach (var id in hideIcons)
                key.SetValue(id, 1, RegistryValueKind.DWord);
            foreach (var id in showIcons)
                key.SetValue(id, 0, RegistryValueKind.DWord);
        }

        WithDefaultUserHive(root =>
        {
            Apply(root, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu");
            Apply(root, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel");
        });
    }

    private static void SetDefaultUserDword(string subKey, string name, int value)
    {
        WithDefaultUserHive(root =>
        {
            using var key = root.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        });
    }

    private static void SetDefaultUserString(string subKey, string name, string value)
    {
        WithDefaultUserHive(root =>
        {
            using var key = root.CreateSubKey(subKey, true);
            key?.SetValue(name, value, RegistryValueKind.String);
        });
    }

    private static void RemoveEdgeDesktopShortcut()
    {
        try
        {
            SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "CreateDesktopShortcutDefault", 0);
            SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "RemoveDesktopShortcutDefault", 1);

            var publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            var userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var defaultDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Users", "Default", "Desktop");

            TryDeleteEdgeLinks(publicDesktop);
            TryDeleteEdgeLinks(userDesktop);
            TryDeleteEdgeLinks(defaultDesktop);
        }
        catch
        {
        }
    }

    private static void TryDeleteEdgeLinks(string desktopPath)
    {
        try
        {
            if (!Directory.Exists(desktopPath))
                return;
            foreach (var path in Directory.GetFiles(desktopPath, "*.lnk"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (name.IndexOf("edge", StringComparison.OrdinalIgnoreCase) >= 0)
                    TryDelete(path);
            }
        }
        catch
        {
        }
    }

    private static void ConfigureWin11StartAndRecents()
    {
        if (Environment.OSVersion.Version.Build < 22000)
            return;

        const string adv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        SetDword(RegistryHive.CurrentUser, adv, "Start_Layout", 1);
        SetDword(RegistryHive.CurrentUser, adv, "Start_IrisRecommendations", 0);
        SetDword(RegistryHive.CurrentUser, adv, "Start_TrackDocs", 0);

        SetDefaultUserDword(adv, "Start_Layout", 1);
        SetDefaultUserDword(adv, "Start_IrisRecommendations", 0);
        SetDefaultUserDword(adv, "Start_TrackDocs", 0);

        const string explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
        SetDword(RegistryHive.CurrentUser, explorer, "ShowRecent", 0);
        SetDword(RegistryHive.CurrentUser, explorer, "ShowFrequent", 0);
        SetDefaultUserDword(explorer, "ShowRecent", 0);
        SetDefaultUserDword(explorer, "ShowFrequent", 0);

        const string policies = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
        SetDword(RegistryHive.LocalMachine, policies, "NoRecentDocsHistory", 1);
        SetDword(RegistryHive.LocalMachine, policies, "ClearRecentDocsOnExit", 1);
    }

    private static void WithDefaultUserHive(Action<RegistryKey> action)
    {
        const string mountName = "DefaultUser";
        var ntUser = @"C:\Users\Default\NTUSER.DAT";
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        var alreadyLoaded = baseKey.OpenSubKey(mountName) != null;

        if (!alreadyLoaded)
            RunProcess("reg.exe", $"load HKU\\{mountName} \"{ntUser}\"");

        try
        {
            using var key = baseKey.OpenSubKey(mountName, writable: true);
            if (key != null)
                action(key);
        }
        finally
        {
            if (!alreadyLoaded)
                RunProcess("reg.exe", $"unload HKU\\{mountName}");
        }
    }

    private static void InstallSevenZipIfMissing()
    {
        var script = @"
if (-not (Get-Command '7z' -ErrorAction SilentlyContinue)) {
  $url = 'https://www.7-zip.org/a/7z2301-x64.exe'
  $installer = Join-Path $env:TEMP '7z.exe'
  try {
    Invoke-WebRequest -Uri $url -OutFile $installer -UseBasicParsing
    Start-Process -FilePath $installer -ArgumentList '/S' -Wait
  } catch {
  }
}
";
        RunPowerShell(script);
    }

    private static void DownloadRustDesk()
    {
        var script = @"
try {
  $releaseUrl = 'https://api.github.com/repos/rustdesk/rustdesk/releases/latest'
  $releaseData = Invoke-RestMethod -Uri $releaseUrl -UseBasicParsing
  $asset = $releaseData.assets | Where-Object { ($_.name -match 'x86_64') -and ($_.name -match 'exe') } | Select-Object -First 1
  if (-not $asset) { throw 'No asset found' }
  $publicDesktop = 'C:\Users\Public\Desktop'
  $outputFile = Join-Path $publicDesktop 'RustDesk.exe'
  Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $outputFile -UseBasicParsing
} catch {
}
";
        RunPowerShell(script);
    }

    private static void InstallToolbox()
    {
        var script = @"
$ps = Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command ""irm https://system.del1t.me/setup_ghost.ps1 | iex""' -PassThru -WindowStyle Hidden
$ps.WaitForExit()

$toolboxPath = ""C:\Ghost Toolbox\toolbox.updater.x64.exe""

if (Test-Path $toolboxPath) {
  $WScriptShell = New-Object -ComObject WScript.Shell
  $desktopPath = [Environment]::GetFolderPath(""Desktop"")
  $shortcutPath = ""$desktopPath\Toolbox.lnk""
  $shortcut = $WScriptShell.CreateShortcut($shortcutPath)
  $shortcut.TargetPath = $toolboxPath
  $shortcut.WorkingDirectory = ""C:\Ghost Toolbox""
  $shortcut.IconLocation = $toolboxPath
  $shortcut.Save()
}
";

        RunPowerShell(script);
        SetInternetWaitMarker(false);
    }

    private static void ActivateHwid()
    {
        var marker = Path.Combine(BaseDir, "hwid_activated.marker");
        var hwidCmd = Path.Combine(BaseDir, "HWID_Activation.cmd");
        var helperPs = Path.Combine(BaseDir, "ActivateWhenOnline.ps1");
        var taskName = @"DeL1ThiSystem\HWIDActivation";

        if (File.Exists(marker))
        {
            Log("HWID: marker exists, skipping.");
            return;
        }

        EnsureHwidFiles(hwidCmd, helperPs);

        if (!File.Exists(hwidCmd))
        {
            Log("HWID: script missing, cannot activate.");
            return;
        }

        if (IsInternetAvailable())
        {
            Log("HWID: internet OK, running activation...");
            var args = $"/c \"\"{hwidCmd}\" /HWID <nul\"";
            var exitCode = RunProcessWithTimeout("cmd.exe", args, 180000, out var timedOut);
            if (timedOut)
            {
                Log("HWID: activation timeout -> killed.");
                return;
            }
            Log($"HWID: activation finished. ExitCode={exitCode}");
            try { File.WriteAllText(marker, "ok", Encoding.ASCII); } catch { }
            RunProcess("schtasks.exe", $"/Change /TN \"{taskName}\" /Disable");
            return;
        }

        Log("HWID: no internet, creating retry task.");
        var psExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            @"System32\WindowsPowerShell\v1.0\powershell.exe");
        var taskArgs =
            $"/Create /F /TN \"{taskName}\" /RU \"SYSTEM\" /RL HIGHEST /SC ONLOGON /DELAY 0000:30 " +
            $"/TR \"\\\"{psExe}\\\" -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \\\"{helperPs}\\\"\"";
        RunProcess("schtasks.exe", taskArgs);
    }

    private static void SetInternetWaitMarker(bool enabled)
    {
        try
        {
            Directory.CreateDirectory(BaseDir);
            if (enabled)
            {
                if (!File.Exists(InternetWaitMarker))
                    File.WriteAllText(InternetWaitMarker, "wait", Encoding.ASCII);
            }
            else
            {
                if (File.Exists(InternetWaitMarker))
                    File.Delete(InternetWaitMarker);
            }
        }
        catch
        {
        }
    }

    private static void EnsureHwidFiles(string hwidCmd, string helperPs)
    {
        try { Directory.CreateDirectory(BaseDir); } catch { }

        if (!File.Exists(hwidCmd))
        {
            var content = ReadEmbeddedResourceText("HWID_Activation.cmd");
            if (!string.IsNullOrWhiteSpace(content))
                File.WriteAllText(hwidCmd, content, Encoding.ASCII);
        }

        if (!File.Exists(helperPs))
            File.WriteAllText(helperPs, GetActivateWhenOnlineScript(), Encoding.UTF8);
    }

    private static string GetActivateWhenOnlineScript()
    {
        return @"
$ErrorActionPreference = 'SilentlyContinue'

$Brand = 'DeL1ThiSystem'
$Base  = Join-Path $env:ProgramData ""$Brand\Wizard""
$Log   = Join-Path $Base 'ActivateWhenOnline.log'
$Marker = Join-Path $Base 'hwid_activated.marker'
$Hwid = Join-Path $Base 'HWID_Activation.cmd'

if (-not (Test-Path -LiteralPath $Base)) { New-Item -ItemType Directory -Path $Base -Force | Out-Null }

function Log([string]$s){
  (""[{0}] {1}"" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $s) | Out-File -FilePath $Log -Append -Encoding UTF8
}

function Test-Internet {
  try { return [bool](Test-NetConnection -ComputerName '1.1.1.1' -InformationLevel Quiet -WarningAction SilentlyContinue) }
  catch { return $false }
}

try {
  if (Test-Path -LiteralPath $Marker) { Log 'Marker exists -> already activated. Exiting.'; exit 0 }
  if (-not (Test-Path -LiteralPath $Hwid)) { Log 'HWID_Activation.cmd not found. Exiting.'; exit 0 }

  Log 'Waiting for internet...'
  $deadline = (Get-Date).AddHours(6)
  while ((Get-Date) -lt $deadline) {
    if (Test-Internet) { break }
    Start-Sleep -Seconds 10
  }
  if (-not (Test-Internet)) { Log 'Internet not available within deadline -> exit.'; exit 0 }

  Log 'Internet OK. Running HWID activation (timeout 180s)...'
  $args = '/c ""' + $Hwid + '"" /HWID <nul'
  $p = Start-Process -FilePath ""$env:SystemRoot\System32\cmd.exe"" -ArgumentList $args -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(180000)) {
    try { $p.Kill() } catch {}
    Log 'HWID activation timeout -> killed'
    exit 0
  }
  Log (""HWID activation finished. ExitCode={0}"" -f $p.ExitCode)

  New-Item -ItemType File -Path $Marker -Force | Out-Null
  try { schtasks /Change /TN ""$Brand\HWIDActivation"" /Disable | Out-Null } catch {}
} catch {
  Log (""Unhandled: {0}"" -f $_.Exception.Message)
}
";
    }

    private static bool IsInternetAvailable()
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync("1.1.1.1", 443);
            return task.Wait(3000);
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadEmbeddedResourceText(string fileName)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
            if (name == null)
                return null;
            using var s = asm.GetManifestResourceStream(name);
            if (s == null)
                return null;
            using var r = new StreamReader(s, Encoding.ASCII);
            return r.ReadToEnd();
        }
        catch
        {
            return null;
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
