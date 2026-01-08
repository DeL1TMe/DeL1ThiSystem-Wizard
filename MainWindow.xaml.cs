using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class MainWindow : Window
{
    private bool _allowClose;
    private bool _executionLock;
    private WindowState _prevState;
    private bool _prevTopmost;

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
        SetExecutionLock(true);
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
        _allowClose = true;
        Close();
    }

    public void SetExecutionLock(bool enabled)
    {
        if (_executionLock == enabled)
            return;

        _executionLock = enabled;

        if (enabled)
        {
            _prevState = WindowState;
            _prevTopmost = Topmost;
            WindowState = WindowState.Maximized;
            Topmost = true;
            RootGrid.Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
            InteractionBlocker.Visibility = Visibility.Visible;
        }
        else
        {
            WindowState = _prevState;
            Topmost = _prevTopmost;
            RootGrid.Background = Brushes.Transparent;
            InteractionBlocker.Visibility = Visibility.Collapsed;
        }
    }
}
