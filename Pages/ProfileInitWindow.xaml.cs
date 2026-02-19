using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using DeL1ThiSystem.ConfigurationWizard.Profile;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class ProfileInitWindow : Window, INotifyPropertyChanged
{
    private readonly string _osFamily;
    private readonly string _themeChoice;
    private readonly (string Id, string Title)[] _steps;
    private string _currentStepText = "Подготовка…";
    private string _percentText = "0%";
    private double _progressWidth;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentStepText { get => _currentStepText; set { _currentStepText = value; OnPropertyChanged(); } }
    public string PercentText { get => _percentText; set { _percentText = value; OnPropertyChanged(); } }
    public double ProgressWidth { get => _progressWidth; set { _progressWidth = value; OnPropertyChanged(); } }

    public ProfileInitWindow(string osFamily)
    {
        _osFamily = osFamily;
        var plan = ProfileInitPlanBuilder.Build(osFamily);
        _themeChoice = plan.ThemeChoice;
        _steps = plan.Steps;

        InitializeComponent();
        DataContext = this;
        Loaded += async (_, __) => await RunAsync();
    }

    private async Task RunAsync()
    {
        if (ProfileSelectionStore.IsAppliedForCurrentUser())
        {
            Close();
            return;
        }

        var total = Math.Max(1, _steps.Length);
        for (var i = 0; i < _steps.Length; i++)
        {
            CurrentStepText = _steps[i].Title;
            SetProgress((double)i / total);
            await Task.Run(() => TweakExecutor.Execute(_steps[i].Id, _osFamily, _themeChoice));
            await Task.Delay(120);
        }

        SetProgress(1);
        CurrentStepText = "Готово";
        await Task.Delay(250);
        ProfileSelectionStore.MarkAppliedForCurrentUser();
        Close();
    }

    private void SetProgress(double p)
    {
        var value = Math.Clamp(p, 0, 1);
        ProgressWidth = 700 * value;
        PercentText = $"{(int)Math.Round(value * 100)}%";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
