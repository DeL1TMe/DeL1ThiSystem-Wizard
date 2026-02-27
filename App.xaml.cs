using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Profile;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class App : Application
{
    public WizardState State { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        var forceRun = e.Args.Any(a => string.Equals(a, "--force-run", StringComparison.OrdinalIgnoreCase));
        State.IsForceRun = forceRun;
        var isAdmin = IsRunningAsAdmin();

        TweakExecutor.LogWizard($"OnStartup: args=[{string.Join(", ", e.Args)}] forceRun={forceRun} admin={isAdmin}");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            TweakExecutor.LogWizard($"UnobservedTaskException: {ex.Exception}");
            try
            {
                MessageBox.Show(ex.Exception.ToString(), "DeL1ThiSystem - Unobserved exception",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }

            ex.SetObserved();
        };

        if (forceRun && !ShouldRunForceModeForCurrentUser())
        {
            TweakExecutor.LogWizard("Shutdown: force-run mode not needed for current user");
            Shutdown(0);
            return;
        }

        if (!forceRun && HasCompletionMarker())
        {
            TweakExecutor.LogWizard("Shutdown: completion marker exists");
            Shutdown(0);
            return;
        }

        if (!forceRun && !isAdmin)
        {
            // If profile selection exists, this is a secondary account flow.
            // Relaunch in force mode without elevation.
            if (HasProfileSelection())
            {
                TweakExecutor.LogWizard("Relaunching in force-run mode (profile selection exists)");
                RelaunchForceRun();
                Shutdown(0);
                return;
            }

            TweakExecutor.LogWizard("Relaunching as admin");
            RelaunchAsAdmin(e.Args);
            Shutdown(0);
            return;
        }

        SuppressEdgeFirstRunNoise();

        State.ThemeChoice = DetectWindowsTheme();
        TweakExecutor.LogSessionStart(State.OsFamily, State.ThemeChoice, forceRun, isAdmin);

        ThemeManager.ApplyTheme(State.ThemeChoice);
        base.OnStartup(e);
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
        TweakExecutor.LogWizard("MainWindow shown");
    }

    private static bool ShouldRunForceModeForCurrentUser()
    {
        try
        {
            if (!File.Exists(ProfileInitPaths.SelectionFile))
                return false;
        }
        catch
        {
            return false;
        }

        try
        {
            if (ProfileSelectionStore.IsAppliedForCurrentUser())
                return false;
        }
        catch
        {
        }

        try
        {
            if (File.Exists(ProfileInitPaths.OwnerUserFile))
            {
                var owner = File.ReadAllText(ProfileInitPaths.OwnerUserFile)?.Trim();
                if (!string.IsNullOrWhiteSpace(owner) &&
                    string.Equals(owner, Environment.UserName, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }
        catch
        {
        }

        return true;
    }

    private static bool IsRunningAsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static void RelaunchAsAdmin(string[] args)
    {
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exe))
                return;

            var escaped = args.Select(a => a.Contains(' ') ? $"\"{a.Replace("\"", "\\\"")}\"" : a);
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(" ", escaped),
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
        }
        catch
        {
        }
    }

    private static void RelaunchForceRun()
    {
        try
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(exe))
                return;

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "--force-run",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
        }
    }

    private static bool HasCompletionMarker()
    {
        try
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DeL1ThiSystem",
                "Wizard");
            var marker = Path.Combine(baseDir, $"completed_{Environment.UserName}.marker");
            return File.Exists(marker);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasProfileSelection()
    {
        try
        {
            return File.Exists(ProfileInitPaths.SelectionFile);
        }
        catch
        {
            return false;
        }
    }

    private static void SuppressEdgeFirstRunNoise()
    {
        try
        {
            using var edgePolicy = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Edge", true);
            edgePolicy?.SetValue("HideFirstRunExperience", 1, RegistryValueKind.DWord);
            edgePolicy?.SetValue("BackgroundModeEnabled", 0, RegistryValueKind.DWord);
            edgePolicy?.SetValue("StartupBoostEnabled", 0, RegistryValueKind.DWord);
            edgePolicy?.SetValue("PromotionalTabsEnabled", 0, RegistryValueKind.DWord);
        }
        catch
        {
        }
    }

    private static string DetectWindowsTheme()
    {
        try
        {
            using var personalize = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);
            var apps = personalize?.GetValue("AppsUseLightTheme");
            if (apps is int i)
                return i == 1 ? "light" : "dark";
        }
        catch
        {
        }

        return "dark";
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        TweakExecutor.LogWizard($"CRASH (DispatcherUnhandledException): {e.Exception}");
        try
        {
            MessageBox.Show(e.Exception.ToString(), "DeL1ThiSystem - Crash",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
        }

        e.Handled = true;
    }
}
