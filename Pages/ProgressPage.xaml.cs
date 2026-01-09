using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class ProgressPage : Page, INotifyPropertyChanged
{
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
    private readonly DispatcherTimer _waitTimer;
    private CancellationTokenSource? _slowStepCts;
    private bool _slowNoticeShown;
    private readonly string _headerTextBase;
    private readonly string _internetWaitMarker = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DeL1ThiSystem",
        "Wizard",
        "waiting_internet.marker");
    private string _currentStepTitleBase = "";

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
        "Примечание: используйте Toolbox для продолжения настройки системы.\n" +
        "Когда закончите — создайте резервную копию в AOMEI Backuper.";

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

        _waitTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _waitTimer.Tick += (_, __) =>
        {
            if (IsCompleted || string.IsNullOrWhiteSpace(_currentStepTitleBase))
                return;
            if (File.Exists(_internetWaitMarker))
                CurrentStepText = $"{_currentStepTitleBase} (ожидание интернета...)";
            else
                CurrentStepText = _currentStepTitleBase;
        };

        Loaded += async (_, __) => await RunAsync();
    }

    private async Task RunAsync()
    {
        int total = Math.Max(1, _steps.Length);
        var start = DateTime.UtcNow;
        try
        {
            _waitTimer.Start();
            for (int i = 0; i < _steps.Length; i++)
            {
                _currentStepTitleBase = _steps[i].Title;
                CurrentStepText = _currentStepTitleBase;
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
                HeaderText = "Задача выполнена";
                CurrentStepText = "Требуется перезагрузка";
                IsCompleted = true;
                if (_showReboot)
                {
                    TryWriteCompletionMarker();
                    TryDeleteWizardTask();
                }
            }

            if (!_showFooter || _autoNavigate)
            {
                _state.BootstrapApplied = true;
                ((MainWindow)Application.Current.MainWindow).NavigateToDisclaimer();
            }
        }
        finally
        {
            _waitTimer.Stop();
            StopSlowNoticeTimer();
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
            FadeHeaderText(_headerTextBase);
            _slowNoticeShown = false;
        }
    }

    private void FadeHeaderText(string newText)
    {
        if (HeaderTextBlock == null || string.Equals(HeaderText, newText, StringComparison.Ordinal))
        {
            HeaderText = newText;
            return;
        }

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.18));
        fadeOut.Completed += (_, __) =>
        {
            HeaderText = newText;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.18));
            HeaderTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        };
        HeaderTextBlock.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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
}
