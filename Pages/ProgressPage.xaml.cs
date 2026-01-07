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

    private string _headerText = "Применяем изменения…";
    private string _currentStepText = "";
    private string _percentText = "0%";
    private double _progressWidth = 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string HeaderText { get => _headerText; set { _headerText = value; OnPropertyChanged(); } }
    public string CurrentStepText { get => _currentStepText; set { _currentStepText = value; OnPropertyChanged(); } }
    public string PercentText { get => _percentText; set { _percentText = value; OnPropertyChanged(); } }
    public double ProgressWidth { get => _progressWidth; set { _progressWidth = value; OnPropertyChanged(); } }

    public Visibility FooterVisible => _showFooter ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RebootVisible => _showReboot ? Visibility.Visible : Visibility.Collapsed;

    public string FooterText { get; set; } =
        "Примечание: используйте Toolbox для продолжения настройки системы.\nПосле рекомендуется создать резервную копию с помощью AOMEI Backuper.";

    public ProgressPage((string Id, string Title)[] steps, string headerText, bool showFooter, bool showReboot)
    {
        InitializeComponent();

        _state = ((App)Application.Current).State;
        _steps = steps;
        _showFooter = showFooter;
        _showReboot = showReboot;

        HeaderText = headerText;
        DataContext = this;

        Loaded += async (_, __) => await RunAsync();
    }

    private async Task RunAsync()
    {
        // UI-only progress simulation. Actual tweak execution is wired in next stage.
        int total = Math.Max(1, _steps.Length);

        for (int i = 0; i < _steps.Length; i++)
        {
            CurrentStepText = _steps[i].Title;
            double p = (double)(i) / total;
            SetProgress(p);
            await Task.Delay(120);
        }

        SetProgress(1);

        // Navigation behavior:
        if (!_showFooter)
        {
            // Bootstrap stage finished -> go to welcome/disclaimer
            _state.BootstrapApplied = true;
            ((MainWindow)Application.Current.MainWindow).NavigateToDisclaimer();
        }
    }

    private void SetProgress(double p)
    {
        p = Math.Clamp(p, 0, 1);
        // Inner fill width after 3px outline and 2px inset per side.
        ProgressWidth = 626 * p;
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
