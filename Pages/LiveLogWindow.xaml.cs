using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class LiveLogWindow : Window
{
    private const double DesignWidth = 720;
    private const double DesignHeight = 800;
    private readonly string _logPath;
    private readonly DispatcherTimer _timer;
    private long _offset;
    private ScrollViewer? _logScrollViewer;

    public LiveLogWindow(string logPath)
    {
        InitializeComponent();
        _logPath = logPath;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _timer.Tick += (_, __) => ReadDelta();

        Loaded += (_, __) =>
        {
            AdjustWindowToWorkArea();
            HookLogScrollViewer();
            ReadDelta();
            _timer.Start();
        };

        Closed += (_, __) =>
        {
            _timer.Stop();
            UnhookLogScrollViewer();
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        };
    }

    private void ReadDelta()
    {
        try
        {
            if (!File.Exists(_logPath))
                return;

            using var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (_offset > stream.Length)
                _offset = 0;

            stream.Position = _offset;
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var delta = reader.ReadToEnd();
            _offset = stream.Position;

            if (string.IsNullOrEmpty(delta))
                return;

            LogTextBox.AppendText(delta);
            if (LogTextBox.Text.Length > 250000)
            {
                LogTextBox.Text = LogTextBox.Text[^200000..];
            }
            LogTextBox.ScrollToEnd();
            UpdateFadeOverlays();
        }
        catch
        {
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
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

    private void HookLogScrollViewer()
    {
        UnhookLogScrollViewer();
        _logScrollViewer = FindDescendant<ScrollViewer>(LogTextBox);
        if (_logScrollViewer == null)
            return;
        _logScrollViewer.ScrollChanged += LogScrollViewer_ScrollChanged;
        UpdateFadeOverlays();
    }

    private void UnhookLogScrollViewer()
    {
        if (_logScrollViewer == null)
            return;
        _logScrollViewer.ScrollChanged -= LogScrollViewer_ScrollChanged;
        _logScrollViewer = null;
    }

    private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        UpdateFadeOverlays();
    }

    private void UpdateFadeOverlays()
    {
        if (_logScrollViewer == null)
            return;

        const double threshold = 1.0;
        var hasTop = _logScrollViewer.VerticalOffset > threshold;
        var hasBottom = _logScrollViewer.VerticalOffset < (_logScrollViewer.ScrollableHeight - threshold);

        if (TopFadeOverlay != null)
            TopFadeOverlay.Opacity = hasTop ? 1.0 : 0.0;
        if (BottomFadeOverlay != null)
            BottomFadeOverlay.Opacity = hasBottom ? 1.0 : 0.0;
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
                return typed;

            var nested = FindDescendant<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
