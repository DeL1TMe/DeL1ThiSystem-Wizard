using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class MainWindow : Window
{
    private bool _allowClose;
    private CancellationTokenSource? _explorerGuardCts;

    public MainWindow()
    {
        InitializeComponent();
        LoadHeaderLogo();
        LoadAppIcon();
    }

    private void LoadHeaderLogo()
    {
        try
        {
            HeaderLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png", UriKind.Absolute));
        }
        catch { }
    }

    private void LoadAppIcon()
    {
        try
        {
            Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/app-icon.png", UriKind.Absolute));
        }
        catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        StartExplorerGuard();
        var app = (App)Application.Current;

        if (!app.State.BootstrapApplied)
        {
            var boot = TweaksJsonLoader.LoadBootstrapSteps();
            Frame.Navigate(new ProgressPage(
                boot.Steps,
                boot.Title,
                showFooter: true,
                showReboot: false,
                footerText: "Появился вопрос? Задай его напрямую через официальный веб‑сайт: del1t.me.\nИстория изменений проекта доступна по адресу: system.del1t.me/changelog.",
                autoNavigate: true));
            return;
        }

        NavigateToDisclaimer();
    }

    public void NavigateToDisclaimer() => Frame.Navigate(new DisclaimerPage());

    public void NavigateToMain() => Frame.Navigate(new MainPage());

    private void Close_Click(object sender, RoutedEventArgs e) => ShowExitConfirm();

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        ShowExitConfirm();
    }

    private void ShowExitConfirm()
    {
        if (ExitConfirmOverlay.Visibility == Visibility.Visible)
            return;

        MainContent.IsEnabled = false;
        MainContent.Effect = new BlurEffect { Radius = 6 };
        ExitConfirmOverlay.Visibility = Visibility.Visible;
    }

    private void ExitConfirmCancel_Click(object sender, RoutedEventArgs e)
    {
        ExitConfirmOverlay.Visibility = Visibility.Collapsed;
        MainContent.IsEnabled = true;
        MainContent.Effect = null;
    }

    private void ExitConfirmExit_Click(object sender, RoutedEventArgs e)
    {
        StopExplorerGuard();
        SetAutoRestartShell(true);
        StartExplorer();
        _allowClose = true;
        Close();
    }

    private void StartExplorerGuard()
    {
        if (_explorerGuardCts != null)
            return;

        SetAutoRestartShell(false);
        _explorerGuardCts = new CancellationTokenSource();
        var token = _explorerGuardCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var p in Process.GetProcessesByName("explorer"))
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                catch
                {
                }

                try
                {
                    await Task.Delay(1500, token);
                }
                catch
                {
                }
            }
        }, token);
    }

    private void StopExplorerGuard()
    {
        try
        {
            _explorerGuardCts?.Cancel();
            _explorerGuardCts?.Dispose();
            _explorerGuardCts = null;
        }
        catch
        {
        }
    }

    private static void StartExplorer()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var proc = Process.Start(psi);
        }
        catch
        {
        }
    }

    private static void SetAutoRestartShell(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true);
            key?.SetValue("AutoRestartShell", enabled ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
        }
        catch
        {
        }
    }
}
