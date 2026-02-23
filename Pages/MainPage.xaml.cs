using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DeL1ThiSystem.ConfigurationWizard.Profile;
using DeL1ThiSystem.ConfigurationWizard.Resources;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class MainPage : Page
{
    private readonly WizardState _state;
    private Popup? _openInfoPopup;
    private Border? _openInfoPopupBorder;

    public ObservableCollection<TweakNode> TweakItems { get; } = new();

    public MainPage()
    {
        _state = ((App)Application.Current).State;

        try
        {
            InitializeComponent();
            DataContext = this;

            LoadTweaksCatalog();
            ApplyThemeSelectionVisuals();

            Loaded += (_, __) =>
            {
                LoadThemeImages();
                UpdateFadeOverlays();
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "DeL1ThiSystem - MainPage init failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTweaksCatalog()
    {
        TweakItems.Clear();
        var nodes = TweaksCatalogLoader.LoadAsNodes(_state.OsFamily);
        var titleComparer = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

        var visibleItems = nodes
            .Where(item =>
                !string.Equals(item.Stage, "bootstrap", StringComparison.OrdinalIgnoreCase) &&
                item.IsEnabled)
            .OrderBy(item => item.Title, titleComparer)
            .ToList();

        foreach (var item in visibleItems)
        {
            TweakItems.Add(item);
        }

        foreach (var item in visibleItems)
        {
            if (!_state.Tweaks.ContainsKey(item.Id))
                _state.Tweaks[item.Id] = item.IsChecked;
        }
    }

    private void LoadThemeImages()
    {
        try
        {
            ThemeLightBrush.ImageSource = LoadBitmapForBrush("pack://application:,,,/Assets/theme_light.png", 300, 172);
            ThemeDarkBrush.ImageSource = LoadBitmapForBrush("pack://application:,,,/Assets/theme_dark.png", 300, 172);
        }
        catch
        {
        }
    }

    private BitmapImage LoadBitmapForBrush(string uri, double targetWidthDip, double targetHeightDip)
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

    private void ApplyThemeSelectionVisuals()
    {
        bool isDark = string.Equals(_state.ThemeChoice, "dark", StringComparison.OrdinalIgnoreCase);

        ThemeLightStroke.Opacity = isDark ? 0.0 : 1.0;
        ThemeDarkStroke.Opacity = isDark ? 1.0 : 0.0;
    }

    private void ThemeLight_Click(object sender, RoutedEventArgs e)
    {
        _state.ThemeChoice = "light";
        ApplyThemeSelectionVisuals();
        ThemeManager.ApplyTheme(_state.ThemeChoice);
        WallpaperManager.ApplyTheme(_state.ThemeChoice);
        TweakExecutor.ApplyWindowsTheme(_state.ThemeChoice);
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _state.ThemeChoice = "dark";
        ApplyThemeSelectionVisuals();
        ThemeManager.ApplyTheme(_state.ThemeChoice);
        WallpaperManager.ApplyTheme(_state.ThemeChoice);
        TweakExecutor.ApplyWindowsTheme(_state.ThemeChoice);
    }

    private void UpdateFadeOverlays()
    {
        double max = MainScroll.ScrollableHeight;
        double y = MainScroll.VerticalOffset;

        TopFadeOverlay.Opacity = y <= 1 ? 0 : 1;

        BottomFadeOverlay.Opacity = (max <= 1 || y >= max - 1) ? 0 : 1;
    }

    private void MainScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateFadeOverlays();
        CloseInfoPopup();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Application.Current.MainWindow).NavigateToDisclaimer();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in TweakItems)
        {
            _state.Tweaks[item.Id] = item.IsChecked;
        }

        var selectedIds = TweakItems
            .Where(i => i.IsChecked)
            .Select(i => i.Id)
            .ToArray();
        if (!_state.IsForceRun)
            TrySaveProfileSelection(_state.ThemeChoice, selectedIds);
        ProfileSelectionStore.MarkAppliedForCurrentUser();

        var steps = TweakItems
            .Where(i => i.IsChecked)
            .Select(i => (i.Id, i.Title))
            .ToArray();

        var themeStep = ("ui.color_theme", "Применяем тему");
        if (!steps.Any(s => string.Equals(s.Id, themeStep.Item1, StringComparison.OrdinalIgnoreCase)))
            steps = new[] { themeStep }.Concat(steps).ToArray();

        if (steps.Length == 0)
            steps = new[] { ("noop", "Применяем твики") };
        else
        {
            var removeUwp = steps
                .Where(s => string.Equals(s.Id, "apps.remove_uwp", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (removeUwp.Length > 0)
            {
                steps = steps
                    .Where(s => !string.Equals(s.Id, "apps.remove_uwp", StringComparison.OrdinalIgnoreCase))
                    .Concat(removeUwp)
                    .ToArray();
            }
        }

        ((MainWindow)Application.Current.MainWindow).Frame.Navigate(
            new ProgressPage(steps, "Применяем выбранные настройки", showFooter: true, showReboot: true, footerText: WizardTexts.SetupFooter));
    }

    private static void TrySaveProfileSelection(string themeChoice, string[] selectedIds)
    {
        try
        {
            ProfileSelectionStore.Save(themeChoice, selectedIds);
            return;
        }
        catch (Exception)
        {
        }

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            if (isAdmin)
            {
                MessageBox.Show(
                    "Не удалось сохранить общий профиль применения твиков.",
                    "DeL1ThiSystem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch
        {
        }
    }

    private void Toggle_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton t || t.DataContext is not TweakNode n) return;
        if (!n.IsEnabled) { t.IsChecked = false; return; }
        n.IsChecked = t.IsChecked == true;
        _state.Tweaks[n.Id] = n.IsChecked;
    }

    private void InfoHit_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not FrameworkElement hit)
            return;

        if (!FindPopupParts(hit, out Popup? popup, out Border? border)
            || popup == null || border == null)
            return;

        if (_openInfoPopup != null && _openInfoPopup != popup)
            CloseInfoPopup();

        _openInfoPopup = popup;
        _openInfoPopupBorder = border;
        _openInfoPopup.IsOpen = true;
        AnimateOpacity(_openInfoPopupBorder, 1.0, 0.12);
    }

    private void InfoHit_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not FrameworkElement hit)
            return;

        if (!FindPopupParts(hit, out Popup? popup, out Border? border)
            || popup == null || border == null)
            return;

        var anim = AnimateOpacity(border, 0.0, 0.12);
        anim.Completed += (_, __) =>
        {
            popup.IsOpen = false;
            if (_openInfoPopup == popup)
            {
                _openInfoPopup = null;
                _openInfoPopupBorder = null;
            }
        };
    }

    private static bool FindPopupParts(FrameworkElement hit, out Popup? popup, out Border? border)
    {
        popup = null;
        border = null;

        if (hit.Parent is not FrameworkElement parent)
            return false;

        popup = parent.FindName("InfoPopup") as Popup;
        border = parent.FindName("InfoPopupBorder") as Border;

        return popup != null && border != null;
    }

    private static DoubleAnimation AnimateOpacity(UIElement target, double to, double seconds)
    {
        var anim = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromSeconds(seconds),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        target.BeginAnimation(UIElement.OpacityProperty, anim);
        return anim;
    }

    private void CloseInfoPopup()
    {
        if (_openInfoPopupBorder != null)
            _openInfoPopupBorder.BeginAnimation(UIElement.OpacityProperty, null);

        if (_openInfoPopup != null)
            _openInfoPopup.IsOpen = false;

        _openInfoPopup = null;
        _openInfoPopupBorder = null;
    }
}
