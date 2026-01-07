﻿﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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

            LoadThemeImages();
            LoadTweaksCatalog();
            ApplyThemeSelectionVisuals();

            MainScroll.ScrollChanged += (_, __) => UpdateFadeOverlays();
            Loaded += (_, __) => UpdateFadeOverlays();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "DeL1ThiSystem - MainPage init failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadTweaksCatalog()
    {
        TweakItems.Clear();
        var nodes = TweaksJsonLoader.LoadAsNodes(_state.OsFamily);
        foreach (var group in nodes)
        {
            foreach (var item in group.Children)
            {
                if (!string.Equals(item.Stage, "bootstrap", StringComparison.OrdinalIgnoreCase))
                    TweakItems.Add(item);
            }
        }

        foreach (var group in nodes)
        {
            foreach (var item in group.Children)
            {
                if (!_state.Tweaks.ContainsKey(item.Id))
                    _state.Tweaks[item.Id] = item.IsChecked;
            }
        }
    }

    private void LoadThemeImages()
    {
        try
        {
            ThemeLightBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/theme_light.png", UriKind.Absolute));
            ThemeDarkBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/theme_dark.png", UriKind.Absolute));
        }
        catch { }
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
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _state.ThemeChoice = "dark";
        ApplyThemeSelectionVisuals();
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

        var steps = TweakItems
            .Where(i => i.IsChecked)
            .Select(i => (i.Id, i.Title))
            .ToArray();

        if (steps.Length == 0)
            steps = new[] { ("noop", "Применяем выбранные настройки") };

        string footerNote =
            "Примечание: используйте Toolbox для продолжения настройки системы.\n" +
            "Когда закончите — создайте резервную копию в AOMEI Backuper.";

        ((MainWindow)Application.Current.MainWindow).Frame.Navigate(
            new ProgressPage(steps, "Применяем твики", showFooter: true, showReboot: true, footerText: footerNote));
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
