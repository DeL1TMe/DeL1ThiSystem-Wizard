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

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class App : Application
{
    public WizardState State { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        var profileInitMode = e.Args.Any(a => string.Equals(a, "--profile-init", StringComparison.OrdinalIgnoreCase));

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
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

        if (profileInitMode)
        {
            if (ProfileSelectionStore.IsAppliedForCurrentUser())
            {
                Shutdown(0);
                return;
            }

            var plan = ProfileInitPlanBuilder.Build(State.OsFamily);
            if (plan.Steps.Length == 0)
            {
                ProfileSelectionStore.MarkAppliedForCurrentUser();
                Shutdown(0);
                return;
            }

            ThemeManager.ApplyTheme(plan.ThemeChoice);
            base.OnStartup(e);
            var profileWindow = new ProfileInitWindow(State.OsFamily);
            MainWindow = profileWindow;
            profileWindow.Show();
            return;
        }

        if (HasCompletionMarker())
        {
            Shutdown(0);
            return;
        }

        if (!IsRunningAsAdmin())
        {
            RelaunchAsAdmin(e.Args);
            Shutdown(0);
            return;
        }

        ThemeManager.ApplyTheme(State.ThemeChoice);
        base.OnStartup(e);
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
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
