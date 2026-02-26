using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    public static void Execute(string id, string osFamily, string themeChoice)
    {
        EnsureLogDir();
        Log($"START {id}");
        try
        {
            if (IsTweakAlreadyApplied(id, osFamily, themeChoice))
            {
                Log($"SKIP {id}: already applied by state check");
                Log($"END {id}");
                return;
            }

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
                    SetString(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\PowerShell", "ExecutionPolicy", "RemoteSigned");
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
                case "bootstrap.sticky_keys_disable":
                    DisableStickyKeys();
                    break;
                case "bootstrap.enhance_pointer_precision_disable":
                    DisableEnhancePointerPrecision();
                    break;
                case "bootstrap.wallpaper_quality_100":
                    SetWallpaperQuality100();
                    break;
                case "bootstrap.configure_ru_ru_locale":
                    ConfigureAutoTimeZone();
                    break;
                case "system.configure_ru_ru_locale_utf8":
                    ConfigureRuRuLocale();
                    break;
                case "system.cleanup_ru_ru_local_packages":
                    CleanupRuRuLocalPackages();
                    break;
                case "profile.sticky_keys_disable":
                    ApplyProfileStickyKeys();
                    break;
                case "profile.enhance_pointer_precision_disable":
                    ApplyProfileEnhancePointerPrecision();
                    break;
                case "profile.wallpaper_quality_100":
                    ApplyProfileWallpaperQuality100();
                    break;
                case "profile.configure_ru_ru_user_locale":
                    ApplyProfileRuRuUserLocale();
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
                    SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowSearchHighlights", 0);
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0);
                    SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableWebSearch", 1);
                    SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchUseWeb", 0);
                    SetDword(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "ConnectedSearchPrivacy", 3);
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
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
                case "perf.visualfx_profile":
                    ApplyVisualFxProfile();
                    break;
                case "perf.memory_integrity_disable":
                    DisableMemoryIntegrity();
                    break;
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
                case "shell.meet_now_disable":
                    DisableMeetNow();
                    break;
                case "shell.search_box_mode":
                    SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1);
                    SetDefaultUserDword(@"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1);
                    break;
                case "shell.start_tiles_clear":
                    ConfigureStartPinsForOs(osFamily);
                    ResetCurrentUserStartState(osFamily);
                    ScheduleStartCleanupOnce();
                    break;
                case "shell.explorer_launch_to_this_pc":
                    SetExplorerLaunchToThisPc();
                    break;
                case "shell.desktop_icons_minimal":
                    SetDesktopIconsMinimal();
                    break;
                case "shell.taskbar_clear_pins":
                    ClearTaskbarPins();
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

}
