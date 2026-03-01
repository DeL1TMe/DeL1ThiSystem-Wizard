using System;
using System.IO;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    // ── shell.taskbar_cleanup ──────────────────────────────────────
    // Combines: hide_task_view, search_box_mode, meet_now_disable,
    //           taskbar_clear_pins, taskbar_end_task, tray_show_all_icons

    private static void CleanupTaskbar(string osFamily)
    {
        const string adv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        const string search = @"Software\Microsoft\Windows\CurrentVersion\Search";

        // Hide Task View button
        SetDword(RegistryHive.CurrentUser, adv, "ShowTaskViewButton", 0);
        SetDefaultUserDword(adv, "ShowTaskViewButton", 0);

        // Collapse search to icon
        SetDword(RegistryHive.CurrentUser, search, "SearchboxTaskbarMode", 1);
        SetDefaultUserDword(search, "SearchboxTaskbarMode", 1);

        // Hide Meet Now (Win10 only)
        if (string.Equals(osFamily, "10", StringComparison.OrdinalIgnoreCase))
        {
            const string policies = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer";
            SetDword(RegistryHive.LocalMachine, policies, "HideSCAMeetNow", 1);
            SetDword(RegistryHive.CurrentUser, policies, "HideSCAMeetNow", 1);
            SetDefaultUserDword(policies, "HideSCAMeetNow", 1);
        }

        // Hide Widgets/TaskbarDa (Win11)
        SetDword(RegistryHive.CurrentUser, adv, "TaskbarDa", 0);
        SetDefaultUserDword(adv, "TaskbarDa", 0);

        // Add "End Task" to taskbar context menu (Win11)
        if (Environment.OSVersion.Version.Build >= 22000)
        {
            const string devSettings = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings";
            SetDword(RegistryHive.CurrentUser, devSettings, "TaskbarEndTask", 1);
            SetDefaultUserDword(devSettings, "TaskbarEndTask", 1);
        }

        // Show all tray icons
        ShowAllTrayIcons();

        // Clear taskbar pins
        ClearTaskbarPins();
    }


    // ── shell.start_menu_cleanup ───────────────────────────────────
    // Combines: start_tiles_clear, win11_start_recommended_disable

    private static void CleanupStartMenu(string osFamily)
    {
        // Clear Start pins/tiles
        ConfigureStartPinsForOs(osFamily);
        ResetCurrentUserStartState(osFamily);
        ScheduleStartCleanupOnce();

        // Disable Win11 recommendations and recent files
        if (Environment.OSVersion.Version.Build >= 22000)
        {
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
    }


    // ── shell.explorer_settings ────────────────────────────────────
    // Combines: show_file_extensions, explorer_launch_to_this_pc

    private static void ConfigureExplorerSettings()
    {
        const string adv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

        // Show file extensions
        SetDword(RegistryHive.CurrentUser, adv, "HideFileExt", 0);
        SetDefaultUserDword(adv, "HideFileExt", 0);

        // Open Explorer to "This PC"
        SetDword(RegistryHive.CurrentUser, adv, "LaunchTo", 1);
        SetDefaultUserDword(adv, "LaunchTo", 1);
    }


    // ── shell.classic_context_menu ─────────────────────────────────

    private static void EnableClassicContextMenu()
    {
        var key = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";
        SetDefaultValue(RegistryHive.CurrentUser, key, "");

        // Also apply to Default User so new profiles get classic menu
        WithDefaultUserHive(root =>
        {
            using var k = root.CreateSubKey(key, true);
            k?.SetValue(null, "", RegistryValueKind.String);
        });
    }


    // ── shell.desktop_icons_minimal ────────────────────────────────

    private static void SetDesktopIconsMinimal()
    {
        Log(">> SetDesktopIconsMinimal");
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

        const string classic = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu";
        const string modern = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";

        foreach (var id in hideIcons)
        {
            SetDword(RegistryHive.CurrentUser, classic, id, 1);
            SetDword(RegistryHive.CurrentUser, modern, id, 1);
            SetDefaultUserDword(classic, id, 1);
            SetDefaultUserDword(modern, id, 1);
        }

        foreach (var id in showIcons)
        {
            SetDword(RegistryHive.CurrentUser, classic, id, 0);
            SetDword(RegistryHive.CurrentUser, modern, id, 0);
            SetDefaultUserDword(classic, id, 0);
            SetDefaultUserDword(modern, id, 0);
        }
    }


    // ── Internal helpers ───────────────────────────────────────────

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


    private static void ClearTaskbarPins()
    {
        Log(">> ClearTaskbarPins");
        var script = @"
$taskbar = Join-Path $env:APPDATA 'Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar';
if (Test-Path $taskbar) {
  Get-ChildItem $taskbar -Filter *.lnk -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue;
}
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband' -Name Favorites,FavoritesResolve,FavoritesChanges,FavoritesRemovedChanges -ErrorAction SilentlyContinue;
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue;
";
        RunPowerShell(script);
    }


    private static void ConfigureStartPinsForOs(string osFamily)
    {
        if (string.Equals(osFamily, "10", StringComparison.OrdinalIgnoreCase))
        {
            ClearStartPinsWin10();
            ClearStartLayoutPolicy();
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
$defaultShell = 'C:\Users\Default\AppData\Local\Microsoft\Windows\Shell';
New-Item -ItemType Directory -Path $defaultShell -Force | Out-Null;
$defaultLayouts = @'
<?xml version='1.0' encoding='utf-8'?>
<FullDefaultLayoutTemplate xmlns='http://schemas.microsoft.com/Start/2014/FullDefaultLayout' xmlns:start='http://schemas.microsoft.com/Start/2014/StartLayout' Version='1'>
  <StartLayoutCollection>
    <StartLayout GroupCellWidth='6' />
  </StartLayoutCollection>
</FullDefaultLayoutTemplate>
'@
$defaultMod = @'
<?xml version='1.0' encoding='utf-8'?>
<LayoutModificationTemplate Version='1' xmlns='http://schemas.microsoft.com/Start/2014/LayoutModification'>
  <LayoutOptions StartTileGroupCellWidth='6' />
  <DefaultLayoutOverride>
    <StartLayoutCollection>
      <StartLayout GroupCellWidth='6' />
    </StartLayoutCollection>
  </DefaultLayoutOverride>
</LayoutModificationTemplate>
'@
$defaultLayouts | Set-Content -LiteralPath (Join-Path $defaultShell 'DefaultLayouts.xml') -Encoding UTF8;
$defaultMod | Set-Content -LiteralPath (Join-Path $defaultShell 'LayoutModification.xml') -Encoding UTF8;
$policyPaths = @(
  'HKCU:\Software\Policies\Microsoft\Windows\Explorer',
  'HKLM:\Software\Policies\Microsoft\Windows\Explorer'
);
foreach ($p in $policyPaths) {
  New-Item -Path $p -Force | Out-Null;
  Set-ItemProperty -Path $p -Name StartLayoutFile -Value $layoutPath -Type String;
  Set-ItemProperty -Path $p -Name LockedStartLayout -Value 1 -Type DWord;
}
" + StartStateCleanupScript + @"
Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue;
Stop-Process -Name StartMenuExperienceHost -Force -ErrorAction SilentlyContinue;
Start-Sleep -Seconds 2;
foreach ($p in $policyPaths) {
  Remove-ItemProperty -Path $p -Name StartLayoutFile,LockedStartLayout -ErrorAction SilentlyContinue;
}
if (Test-Path $layoutPath) { Remove-Item $layoutPath -Force -ErrorAction SilentlyContinue; }
";
        RunPowerShell(script);
    }


    private static void ResetCurrentUserStartState(string osFamily)
    {
        if (!string.Equals(osFamily, "10", StringComparison.OrdinalIgnoreCase))
            return;

        var script = @"
Stop-Process -Name explorer,StartMenuExperienceHost,ShellExperienceHost -Force -ErrorAction SilentlyContinue;
" + StartStateCleanupScript;
        RunPowerShell(script);
    }


    /// <summary>Shared script fragment for cleaning Start menu caches.</summary>
    private const string StartStateCleanupScript = @"
Remove-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\Cache\DefaultAccount' -Recurse -Force -ErrorAction SilentlyContinue;
Remove-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount' -Recurse -Force -ErrorAction SilentlyContinue;
$smh = Join-Path $env:LOCALAPPDATA 'Packages\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\LocalState';
if (Test-Path $smh) { Remove-Item -Path (Join-Path $smh '*') -Recurse -Force -ErrorAction SilentlyContinue; }
$tileDb = Join-Path $env:LOCALAPPDATA 'TileDataLayer\Database';
if (Test-Path $tileDb) { Remove-Item $tileDb -Recurse -Force -ErrorAction SilentlyContinue; }
$layouts = @(
  (Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Shell\DefaultLayouts.xml'),
  (Join-Path $env:LOCALAPPDATA 'Microsoft\Windows\Shell\LayoutModification.xml')
);
foreach ($l in $layouts) { if (Test-Path $l) { Remove-Item $l -Force -ErrorAction SilentlyContinue; } }
";


    private static void ScheduleStartCleanupOnce()
    {
        var script = $@"
$taskName = 'DeL1ThiSystem\StartCleanupOnce';
$ps = @'
$taskName = 'DeL1ThiSystem\StartCleanupOnce';
try {{
  schtasks /Delete /TN $taskName /F >$null 2>$null;
}} catch {{}}
Stop-Process -Name explorer,StartMenuExperienceHost,ShellExperienceHost -Force -ErrorAction SilentlyContinue;
{StartStateCleanupScript.Replace("'", "''")}
try {{
  schtasks /Delete /TN $taskName /F >$null 2>$null;
}} catch {{}}
'@;
$path = Join-Path $env:ProgramData 'DeL1ThiSystem\Wizard\StartCleanupOnce.ps1';
New-Item -ItemType Directory -Path (Split-Path $path) -Force | Out-Null;
$ps | Set-Content -LiteralPath $path -Encoding UTF8;
$tr = 'powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File ' + [char]34 + $path + [char]34;
schtasks /Create /F /TN $taskName /RU $env:USERNAME /SC ONLOGON /RL HIGHEST /TR $tr | Out-Null;
";
        RunPowerShell(script);
    }


    private static void ClearStartLayoutPolicy()
    {
        const string key = @"Software\Policies\Microsoft\Windows\Explorer";
        TryDeleteRegistryValue(RegistryHive.CurrentUser, key, "StartLayoutFile");
        TryDeleteRegistryValue(RegistryHive.CurrentUser, key, "LockedStartLayout");
        TryDeleteRegistryValue(RegistryHive.LocalMachine, key, "StartLayoutFile");
        TryDeleteRegistryValue(RegistryHive.LocalMachine, key, "LockedStartLayout");

        WithDefaultUserHive(root =>
        {
            using var k = root.OpenSubKey(key, true);
            if (k == null)
                return;
            k.DeleteValue("StartLayoutFile", false);
            k.DeleteValue("LockedStartLayout", false);
        });

        TryDelete(Path.Combine(BaseDir, "StartLayout.xml"));
    }


    private static void TryDeleteRegistryValue(RegistryHive hive, string subKey, string name)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey, true);
            key?.DeleteValue(name, false);
        }
        catch (Exception ex)
        {
            Log($"TryDeleteRegistryValue failed: {hive}\\{subKey} {name}: {ex.Message}");
        }
    }


    private static void RemoveEdgeDesktopShortcut()
    {
        Log(">> RemoveEdgeDesktopShortcut");
        try
        {
            SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "CreateDesktopShortcutDefault", 0);
            SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "RemoveDesktopShortcutDefault", 1);

            var publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            var userDesktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var defaultDesktop = @"C:\Users\Default\Desktop";

            TryDeleteEdgeLinks(publicDesktop);
            TryDeleteEdgeLinks(userDesktop);
            TryDeleteEdgeLinks(defaultDesktop);
        }
        catch (Exception ex)
        {
            Log($"RemoveEdgeDesktopShortcut ERROR: {ex.Message}");
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

}
