using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class MainWindow : Window
{
    private bool _allowClose;

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
        StartExplorer();
        _allowClose = true;
        Close();
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
}
