using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using DeL1ThiSystem.ConfigurationWizard.Resources;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class ProgressPage : Page, INotifyPropertyChanged
{
    private static LiveLogWindow? _sharedLogWindow;
    private readonly WizardState _state;
    private readonly (string Id, string Title)[] _steps;
    private readonly bool _showFooter;
    private readonly bool _showReboot;
    private readonly bool _autoNavigate;
    private bool _footerDismissed = false;
    private string _headerText = "Ведётся подготовка ОС для дальнейшей настройки…";
    private string _currentStepText = "";
    private string _percentText = "0%";
    private double _progressWidth = 0;
    private bool _rebootEnabled = false;
    private bool _isCompleted = false;
    private bool _internetRequired = false;
    private CancellationTokenSource? _slowStepCts;
    private bool _slowNoticeShown;
    private readonly string _headerTextBase;
    private CancellationTokenSource? _internetMonitorCts;
    private int _onlineStreak;
    private int _offlineStreak;
    private static readonly string _logPath = TweakExecutor.LogPath;
    private MainWindow? _hostWindow;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HeaderText { get => _headerText; set { _headerText = value; OnPropertyChanged(); } }
    public string CurrentStepText { get => _currentStepText; set { _currentStepText = value; OnPropertyChanged(); } }
    public string PercentText { get => _percentText; set { _percentText = value; OnPropertyChanged(); } }
    public double ProgressWidth { get => _progressWidth; set { _progressWidth = value; OnPropertyChanged(); } }
    public bool RebootEnabled { get => _rebootEnabled; set { _rebootEnabled = value; OnPropertyChanged(); } }
    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            _isCompleted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressVisibility));
        }
    }
    public Visibility ProgressVisibility => _isCompleted ? Visibility.Collapsed : Visibility.Visible;
    public Visibility FooterVisibility => (_showFooter && !_footerDismissed) ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RebootVisible => _showReboot ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FooterDismissVisible => (_showFooter && !_showReboot) ? Visibility.Visible : Visibility.Collapsed;

    public string FooterText { get; set; } =
        WizardTexts.SetupFooter;

    public ProgressPage((string Id, string Title)[] steps, string headerText, bool showFooter, bool showReboot, string? footerText = null, bool autoNavigate = false)
    {
        InitializeComponent();

        _state = ((App)Application.Current).State;
        _steps = steps;
        _showFooter = showFooter;
        _showReboot = showReboot;
        _autoNavigate = autoNavigate;

        HeaderText = headerText;
        _headerTextBase = headerText;
        if (!string.IsNullOrWhiteSpace(footerText))
            FooterText = footerText;
        DataContext = this;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        AttachHostWindowHandler();
        await RunAsync();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        DetachHostWindowHandler();
    }

    private void AttachHostWindowHandler()
    {
        if (_hostWindow != null)
            return;
        if (Application.Current.MainWindow is not MainWindow mw)
            return;
        _hostWindow = mw;
        _hostWindow.Closing += HostWindow_Closing;
    }

    private void DetachHostWindowHandler()
    {
        if (_hostWindow == null)
            return;
        _hostWindow.Closing -= HostWindow_Closing;
        _hostWindow = null;
    }

    private void HostWindow_Closing(object? sender, CancelEventArgs e)
    {
        CloseSharedLogWindow();
    }

    private async Task RunAsync()
    {
        TweakExecutor.SetInternetGate(WaitForInternetGateFromWorker);
        StartInternetMonitor();
        await EnsureInternetOrBlockAsync();

        int total = Math.Max(1, _steps.Length);
        var start = DateTime.UtcNow;
        try
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                await EnsureInternetOrBlockAsync();
                CurrentStepText = _steps[i].Title;
                StartSlowNoticeTimer();
                double p = (double)(i) / total;
                SetProgress(p);
                if (!string.Equals(_steps[i].Id, "noop", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Run(() => TweakExecutor.Execute(_steps[i].Id, _state.OsFamily, _state.ThemeChoice));
                }
                StopSlowNoticeTimer();
                await Task.Delay(150);
            }

            SetProgress(1);
            var elapsed = (int)(DateTime.UtcNow - start).TotalMilliseconds;
            if (elapsed < 800)
                await Task.Delay(800 - elapsed);
            if (_showReboot)
                RebootEnabled = true;
            if (_showFooter)
            {
                SetCompletionState();
                if (_showReboot)
                {
                    TryWriteCompletionMarker();
                    TryDeleteWizardTask();
                }
            }

            if (_autoNavigate)
            {
                _state.BootstrapApplied = true;
                ((MainWindow)Application.Current.MainWindow).NavigateToDisclaimer();
            }
        }
        finally
        {
            DetachHostWindowHandler();
            StopInternetMonitor();
            ShowInternetRequiredOverlay(false);
            TweakExecutor.SetInternetGate(null);
            StopSlowNoticeTimer();
        }
    }

    private void ToggleLog_Click(object sender, RoutedEventArgs e)
    {
        if (_sharedLogWindow != null && _sharedLogWindow.IsVisible)
        {
            _sharedLogWindow.Close();
            _sharedLogWindow = null;
            return;
        }

        _sharedLogWindow = new LiveLogWindow(_logPath);
        _sharedLogWindow.Closed += (_, __) => _sharedLogWindow = null;
        _sharedLogWindow.Show();
    }

    private void WaitForInternetGateFromWorker()
    {
        if (Dispatcher.CheckAccess())
            return;
        Dispatcher.InvokeAsync(async () => await EnsureInternetOrBlockAsync()).Task.GetAwaiter().GetResult();
    }

    private async Task EnsureInternetOrBlockAsync()
    {
        _onlineStreak = 0;
        _offlineStreak = 0;
        while (!await HasInternetAsync())
        {
            ShowInternetRequiredOverlay(true);
            await Task.Delay(1200);
        }
        ShowInternetRequiredOverlay(false);
    }

    private void StartInternetMonitor()
    {
        StopInternetMonitor();
        _internetMonitorCts = new CancellationTokenSource();
        var token = _internetMonitorCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var online = await HasInternetAsync();
                await Dispatcher.InvokeAsync(() =>
                {
                    if (online)
                    {
                        _onlineStreak++;
                        _offlineStreak = 0;
                        if (_internetRequired && _onlineStreak >= 1)
                            ShowInternetRequiredOverlay(false);
                        return;
                    }

                    _offlineStreak++;
                    _onlineStreak = 0;
                    if (_offlineStreak >= 2)
                        ShowInternetRequiredOverlay(true);
                });

                try
                {
                    await Task.Delay(1600, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    private void StopInternetMonitor()
    {
        _internetMonitorCts?.Cancel();
        _internetMonitorCts?.Dispose();
        _internetMonitorCts = null;
    }

    private async Task<bool> HasInternetAsync()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1400));
            using var client = new TcpClient();
            await client.ConnectAsync("1.1.1.1", 443, cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ShowInternetRequiredOverlay(bool visible)
    {
        if (_internetRequired == visible)
            return;
        _internetRequired = visible;
        if (Application.Current.MainWindow is MainWindow mw)
        {
            mw.SetInteractionLock(visible);
            mw.SetInternetRequiredOverlay(visible);
        }
    }

    private void SetProgress(double p)
    {
        p = Math.Clamp(p, 0, 1);
        ProgressWidth = 628 * p;
        PercentText = $"{(int)Math.Round(p * 100)}%";
    }

    private void Reboot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CloseSharedLogWindow();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/r /t 0",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch
        {
            MessageBox.Show("Не удалось выполнить перезагрузку.", "DeL1ThiSystem", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void HideFooter_Click(object sender, RoutedEventArgs e)
    {
        _footerDismissed = true;
        OnPropertyChanged(nameof(FooterVisibility));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void StartSlowNoticeTimer()
    {
        _slowNoticeShown = false;
        _slowStepCts?.Cancel();
        _slowStepCts?.Dispose();
        _slowStepCts = new CancellationTokenSource();
        var token = _slowStepCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);
            }
            catch
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            Dispatcher.Invoke(() =>
            {
                if (IsCompleted)
                    return;
                _slowNoticeShown = true;
                FadeHeaderText("Нет, мы не зависли — ожидайте...");
            });
        });
    }

    private void StopSlowNoticeTimer()
    {
        _slowStepCts?.Cancel();
        _slowStepCts?.Dispose();
        _slowStepCts = null;

        if (_slowNoticeShown && !IsCompleted)
        {
            FadeHeaderText(_headerTextBase, force: true);
            _slowNoticeShown = false;
        }
    }

    private void FadeHeaderText(string newText, bool force = false)
    {
        if (HeaderTextBlock == null || (!force && string.Equals(HeaderText, newText, StringComparison.Ordinal)))
        {
            HeaderText = newText;
            return;
        }

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.18));
        fadeOut.Completed += (_, __) =>
        {
            if (IsCompleted)
            {
                HeaderTextBlock.BeginAnimation(UIElement.OpacityProperty, null);
                HeaderTextBlock.Opacity = 1;
                return;
            }
            HeaderText = newText;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.18));
            HeaderTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        };
        HeaderTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }

    private void SetCompletionState()
    {
        if (HeaderTextBlock != null)
        {
            HeaderTextBlock.BeginAnimation(UIElement.OpacityProperty, null);
            HeaderTextBlock.Opacity = 1;
        }
        HeaderText = WizardTexts.CompletionHeader;
        CurrentStepText = WizardTexts.CompletionStep;
        IsCompleted = true;
    }

    private static void TryWriteCompletionMarker()
    {
        try
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "DeL1ThiSystem",
                "Wizard");
            Directory.CreateDirectory(baseDir);
            var marker = Path.Combine(baseDir, $"completed_{Environment.UserName}.marker");
            if (!File.Exists(marker))
                File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
        }
        catch
        {
        }
    }

    private static void TryDeleteWizardTask()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/Delete /TN \"DeL1ThiSystem\\Wizard\" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch
        {
        }
    }

    public static void CloseSharedLogWindow()
    {
        try
        {
            if (_sharedLogWindow != null && _sharedLogWindow.IsVisible)
                _sharedLogWindow.Close();
            _sharedLogWindow = null;
        }
        catch
        {
        }
    }
}
