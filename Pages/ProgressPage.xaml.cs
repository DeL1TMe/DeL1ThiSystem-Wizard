using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        "Перед продолжением создайте резервную копию в AOMEI Backuper.";

    public ProgressPage((string Id, string Title)[] steps, string headerText, bool showFooter, bool showReboot, string? footerText = null, bool autoNavigate = false)
    {
        InitializeComponent();

        _state = ((App)Application.Current).State;
        _steps = steps;
        _showFooter = showFooter;
        _showReboot = showReboot;
        _autoNavigate = autoNavigate;

        HeaderText = headerText;
        if (!string.IsNullOrWhiteSpace(footerText))
            FooterText = footerText;
        DataContext = this;

        Loaded += async (_, __) => await RunAsync();
    }

    private async Task RunAsync()
    {
        int total = Math.Max(1, _steps.Length);

        for (int i = 0; i < _steps.Length; i++)
        {
            CurrentStepText = _steps[i].Title;
            double p = (double)(i) / total;
            SetProgress(p);
            await Task.Delay(120);
        }

        SetProgress(1);
        if (_showReboot)
            RebootEnabled = true;
        if (_showFooter)
        {
            HeaderText = "Задача выполнена";
            CurrentStepText = "Требуется перезагрузка";
            IsCompleted = true;
        }

        if (!_showFooter || _autoNavigate)
        {
            _state.BootstrapApplied = true;
            ((MainWindow)Application.Current.MainWindow).NavigateToDisclaimer();
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
}
