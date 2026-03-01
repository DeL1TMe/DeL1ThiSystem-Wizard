using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Tweaks;

public static partial class TweakExecutor
{

    public static void Execute(string id, string osFamily, string themeChoice)
    {
        EnsureLogDir();
        var sw = Stopwatch.StartNew();
        Log($"START {id}");
        try
        {
            var alreadyApplied = IsTweakAlreadyApplied(id, osFamily, themeChoice);
            Log($"STATE-CHECK {id}: alreadyApplied={alreadyApplied}");
            if (alreadyApplied)
            {
                sw.Stop();
                Log($"SKIP {id}: already applied ({sw.ElapsedMilliseconds}ms)");
                return;
            }

            switch (id)
            {
                // ── Bootstrap ──────────────────────────────────────────
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
                case "bootstrap.restore_disable_cleanup":
                    DisableRestoreAndCleanup();
                    break;

                // ── Locale (internal steps) ────────────────────────────
                case "system.configure_ru_ru_locale_utf8":
                    ConfigureRuRuLocale();
                    break;
                case "system.cleanup_ru_ru_local_packages":
                    CleanupRuRuLocalPackages();
                    break;

                // ── Profile (force-run for secondary users) ────────────
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

                // ── Apps ───────────────────────────────────────────────
                case "apps.edge_restrict":
                    RestrictEdge();
                    break;
                case "apps.onedrive_remove":
                    RemoveOneDriveArtifacts();
                    break;
                case "apps.remove_components":
                    RemoveSystemComponents();
                    break;
                case "apps.remove_uwp":
                    RemoveAppxPackages();
                    break;

                // ── Privacy ────────────────────────────────────────────
                case "privacy.disable_tracking":
                    DisableTrackingAndAds(osFamily);
                    break;
                case "privacy.pause_updates":
                    PauseWindowsUpdate();
                    break;

                // ── Performance ────────────────────────────────────────
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

                // ── Shell ──────────────────────────────────────────────
                case "shell.taskbar_cleanup":
                    CleanupTaskbar(osFamily);
                    break;
                case "shell.start_menu_cleanup":
                    CleanupStartMenu(osFamily);
                    break;
                case "shell.explorer_settings":
                    ConfigureExplorerSettings();
                    break;
                case "shell.desktop_icons_minimal":
                    SetDesktopIconsMinimal();
                    break;
                case "shell.classic_context_menu":
                    EnableClassicContextMenu();
                    break;

                // ── Theme ──────────────────────────────────────────────
                case "ui.color_theme":
                    ApplyWindowsTheme(themeChoice);
                    break;

                // ── Extras ─────────────────────────────────────────────
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
        sw.Stop();
        Log($"END {id} ({sw.ElapsedMilliseconds}ms)");
    }

}
