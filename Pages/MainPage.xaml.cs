﻿﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class MainPage : Page
{
    private readonly WizardState _state;

    public ObservableCollection<TweakNode> TweakGroups { get; } = new();

    public MainPage()
    {
        // Ensure non-nullable field is always initialized (avoids CS8618 warnings)
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
        TweakGroups.Clear();
        var nodes = TweaksJsonLoader.LoadAsNodes(_state.OsFamily);
        foreach (var n in nodes) TweakGroups.Add(n);

        // Initialize state dict with defaults
        foreach (var group in TweakGroups)
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
        // Assets path from user requirement
        string assetsRoot = @"C:\ProgramData\DeL1ThiSystem\Wizard\Assets";
        string light = Path.Combine(assetsRoot, "theme_light.png");
        string dark = Path.Combine(assetsRoot, "theme_dark.png");

        if (File.Exists(light))
            ThemeLightBrush.ImageSource = new BitmapImage(new Uri(light, UriKind.Absolute));
        if (File.Exists(dark))
            ThemeDarkBrush.ImageSource = new BitmapImage(new Uri(dark, UriKind.Absolute));
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
        // TODO: apply wallpaper/lockscreen + refresh explorer on click
    }

    private void ThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _state.ThemeChoice = "dark";
        ApplyThemeSelectionVisuals();
        // TODO: apply wallpaper/lockscreen + refresh explorer on click
    }

    private void UpdateFadeOverlays()
    {
        double max = MainScroll.ScrollableHeight;
        double y = MainScroll.VerticalOffset;

        // Top fade appears after user scrolls down a bit
        TopFadeOverlay.Opacity = y <= 1 ? 0 : 1;

        // Bottom fade disappears when scrolled to bottom
        BottomFadeOverlay.Opacity = (max <= 1 || y >= max - 1) ? 0 : 1;
    }

    // XAML hook: ScrollViewer.ScrollChanged="MainScroll_ScrollChanged"
    private void MainScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateFadeOverlays();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ((MainWindow)Application.Current.MainWindow).NavigateToDisclaimer();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        // Persist current selection
        foreach (var group in TweakGroups)
        {
            foreach (var item in group.Children)
            {
                _state.Tweaks[item.Id] = item.IsChecked;
            }
        }

        var steps = TweakGroups
            .SelectMany(g => g.Children)
            .Where(i => i.IsChecked)
            .Select(i => (i.Id, i.Title))
            .ToArray();

        if (steps.Length == 0)
            steps = new[] { ("noop", "Применяем выбранные настройки") };

        string footerNote =
            "Примечание: используйте Toolbox для продолжения настройки системы.\n" +
            "Перед продолжением создайте резервную копию в AOMEI Backuper.";

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
}
