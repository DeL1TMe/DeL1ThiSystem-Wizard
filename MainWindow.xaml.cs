using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class MainWindow : Window
{
    private const double DesignWidth = 720;
    private const double DesignHeight = 800;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        LoadHeaderLogo();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void LoadHeaderLogo()
    {
        try
        {
            HeaderLogo.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/logo.png", UriKind.Absolute));
        }
        catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AdjustWindowToWorkArea();
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
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _allowClose = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.OnClosed(e);
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void AdjustWindowToWorkArea()
    {
        try
        {
            var work = SystemParameters.WorkArea;
            var targetWidth = Math.Min(DesignWidth, work.Width);
            var targetHeight = Math.Min(DesignHeight, work.Height);
            Width = targetWidth;
            Height = targetHeight;
            Left = work.Left + (work.Width - targetWidth) / 2;
            Top = work.Top + (work.Height - targetHeight) / 2;
        }
        catch
        {
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(AdjustWindowToWorkArea);
    }
}
