using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class App : Application
{
    public WizardState State { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        if (HasCompletionMarker())
        {
            Shutdown(0);
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            try
            {
                MessageBox.Show(ex.Exception.ToString(), "DeL1ThiSystem - Unobserved exception",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }

            ex.SetObserved();
        };

        base.OnStartup(e);
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
        catch { }
        e.Handled = true;
    }
}
