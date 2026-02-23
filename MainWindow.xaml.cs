using System;
using System.Security.Principal;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeL1ThiSystem.ConfigurationWizard.Resources;
using Microsoft.Win32;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class MainWindow : Window
{
    private const double DesignWidth = 720;
    private const double DesignHeight = 800;
    private bool _allowClose;
    private bool _interactionLocked;
    private bool _internetOverlayVisible;

    public MainWindow()
    {
        InitializeComponent();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void LoadHeaderLogo()
    {
        try
        {
            HeaderLogo.Source = LoadBitmapForDisplay("pack://application:,,,/Assets/logo.png", 290, 80);
        }
        catch
        {
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AdjustWindowToWorkArea();
        LoadHeaderLogo();
        var app = (App)Application.Current;

        if (app.State.IsForceRun)
        {
            Hide();
            try
            {
                var profileInit = new ProfileInitWindow(app.State.OsFamily)
                {
                    Owner = this
                };
                profileInit.ShowDialog();
            }
            finally
            {
                _allowClose = true;
                Close();
            }

            return;
        }

        if (!app.State.BootstrapApplied)
        {
            if (!TryValidateBootstrapPrerequisites(app.State.OsFamily, out var boot, out var error))
            {
                MessageBox.Show(error, "DeL1ThiSystem", MessageBoxButton.OK, MessageBoxImage.Error);
                _allowClose = true;
                Close();
                return;
            }

            Frame.Navigate(new ProgressPage(
                boot.Steps,
                boot.Title,
                showFooter: true,
                showReboot: false,
                footerText: WizardTexts.BootstrapFooter,
                autoNavigate: true));
            return;
        }

        NavigateToDisclaimer();
    }

    private static bool TryValidateBootstrapPrerequisites(
        string osFamily,
        out (string Title, (string Id, string Title)[] Steps) bootstrap,
        out string error)
    {
        bootstrap = default;
        error = string.Empty;

        if (!IsRunningAsAdministrator())
        {
            error = "Для запуска первичной настройки приложение должно быть запущено от имени администратора.";
            return false;
        }

        try
        {
            bootstrap = TweaksCatalogLoader.LoadBootstrapSteps();
            _ = TweaksCatalogLoader.LoadAsNodes(osFamily);

            if (bootstrap.Steps.Length == 0)
            {
                error = "Не найдены шаги bootstrap в ресурсах приложения.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = "Ошибка проверки ресурсов приложения перед bootstrap:\n" + ex.Message;
            return false;
        }
    }

    private static bool IsRunningAsAdministrator()
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

    public void NavigateToDisclaimer() => Frame.Navigate(new DisclaimerPage());

    private void Close_Click(object sender, RoutedEventArgs e) => ShowExitConfirm();

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_interactionLocked)
        {
            e.Cancel = true;
            return;
        }

        if (_allowClose)
            return;

        e.Cancel = true;
        ShowExitConfirm();
    }

    private void ShowExitConfirm()
    {
        if (_interactionLocked)
            return;

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
        ProgressPage.CloseSharedLogWindow();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _allowClose = true;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        ProgressPage.CloseSharedLogWindow();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        base.OnClosed(e);
    }

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_interactionLocked)
            return;

        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    public void SetInteractionLock(bool locked)
    {
        _interactionLocked = locked;

        if (locked)
        {
            if (ExitConfirmOverlay.Visibility == Visibility.Visible)
            {
                ExitConfirmOverlay.Visibility = Visibility.Collapsed;
                MainContent.IsEnabled = true;
                MainContent.Effect = null;
            }

            if (MainContent != null)
            {
                MainContent.IsEnabled = false;
                MainContent.Effect = new BlurEffect { Radius = 6 };
            }

            if (HeaderBar != null)
                HeaderBar.Effect = new BlurEffect { Radius = 6 };
            if (HeaderCloseButton != null)
            {
                HeaderCloseButton.IsEnabled = false;
                HeaderCloseButton.Opacity = 0.45;
            }
            return;
        }

        if (MainContent != null)
        {
            MainContent.IsEnabled = true;
            MainContent.Effect = null;
        }

        if (HeaderBar != null)
            HeaderBar.Effect = null;
        if (HeaderCloseButton != null)
        {
            HeaderCloseButton.IsEnabled = true;
            HeaderCloseButton.Opacity = 1;
        }
    }

    public void SetInternetRequiredOverlay(bool visible)
    {
        if (_internetOverlayVisible == visible)
            return;

        _internetOverlayVisible = visible;
        if (InternetRequiredOverlay == null)
            return;

        var duration = TimeSpan.FromMilliseconds(180);
        if (visible)
        {
            InternetRequiredOverlay.Visibility = Visibility.Visible;

            if (InternetRequiredBackdrop != null)
            {
                InternetRequiredBackdrop.Opacity = 0;
                var backdropIn = new DoubleAnimation(0.0, 1.0, duration);
                InternetRequiredBackdrop.BeginAnimation(UIElement.OpacityProperty, backdropIn);
            }

            if (InternetRequiredBlinkText != null)
            {
                var blink = new DoubleAnimation(1.0, 0.35, TimeSpan.FromSeconds(1.1))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                InternetRequiredBlinkText.BeginAnimation(UIElement.OpacityProperty, blink);
            }
            return;
        }

        if (InternetRequiredBackdrop != null)
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, duration);
            fadeOut.Completed += (_, __) =>
            {
                if (!_internetOverlayVisible)
                {
                    InternetRequiredOverlay.Visibility = Visibility.Collapsed;
                    InternetRequiredBackdrop.Opacity = 0;
                }
            };
            InternetRequiredBackdrop.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else
        {
            InternetRequiredOverlay.Visibility = Visibility.Collapsed;
        }

        if (InternetRequiredBlinkText != null)
        {
            InternetRequiredBlinkText.BeginAnimation(UIElement.OpacityProperty, null);
            InternetRequiredBlinkText.Opacity = 1.0;
        }
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
        Dispatcher.Invoke(() =>
        {
            AdjustWindowToWorkArea();
            LoadHeaderLogo();
        });
    }

    private BitmapImage LoadBitmapForDisplay(string uri, double targetWidthDip, double targetHeightDip)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var decodeWidth = Math.Max(1, (int)Math.Round(targetWidthDip * dpi.DpiScaleX));
        var decodeHeight = Math.Max(1, (int)Math.Round(targetHeightDip * dpi.DpiScaleY));
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(uri, UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.DecodePixelWidth = decodeWidth;
        bitmap.DecodePixelHeight = decodeHeight;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
