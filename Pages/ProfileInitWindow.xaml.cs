using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using DeL1ThiSystem.ConfigurationWizard.Profile;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard.Pages;

public partial class ProfileInitWindow : Window, INotifyPropertyChanged
{
    private readonly string _osFamily;
    private readonly string _themeChoice;
    private readonly (string Id, string Title)[] _steps;
    private string _currentStepText = "Подготовка...";
    private string _percentText = "0%";
    private double _progressWidth;
    private bool _internetRequired;

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

        TweakExecutor.SetInternetGate(WaitForInternetGateFromWorker);
        await EnsureInternetOrBlockAsync();

        try
        {
            var total = Math.Max(1, _steps.Length);
            for (var i = 0; i < _steps.Length; i++)
            {
                await EnsureInternetOrBlockAsync();
                CurrentStepText = _steps[i].Title;
                SetProgress((double)i / total);
                await Task.Run(() => TweakExecutor.Execute(_steps[i].Id, _osFamily, _themeChoice));
                await Task.Delay(120);
            }
        }
        finally
        {
            ShowInternetRequiredOverlay(false);
            TweakExecutor.SetInternetGate(null);
        }

        SetProgress(1);
        CurrentStepText = "Готово";
        await Task.Delay(250);
        ProfileSelectionStore.MarkAppliedForCurrentUser();
        Close();
    }

    private void WaitForInternetGateFromWorker()
    {
        if (Dispatcher.CheckAccess())
            return;

        Dispatcher.InvokeAsync(async () => await EnsureInternetOrBlockAsync()).Task.GetAwaiter().GetResult();
    }

    private async Task EnsureInternetOrBlockAsync()
    {
        while (!await HasInternetAsync())
        {
            ShowInternetRequiredOverlay(true);
            await Task.Delay(1200);
        }

        ShowInternetRequiredOverlay(false);
    }

    private async Task<bool> HasInternetAsync()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(900));
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
        if (InternetRequiredOverlay == null)
            return;

        var duration = TimeSpan.FromMilliseconds(180);
        if (visible)
        {
            InternetRequiredOverlay.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(InternetRequiredOverlay.Opacity, 1.0, duration);
            InternetRequiredOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            if (InternetRequiredText != null)
            {
                var blink = new DoubleAnimation(1.0, 0.35, TimeSpan.FromSeconds(1.1))
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                InternetRequiredText.BeginAnimation(UIElement.OpacityProperty, blink);
            }
        }
        else
        {
            var fadeOut = new DoubleAnimation(InternetRequiredOverlay.Opacity, 0.0, duration);
            fadeOut.Completed += (_, __) =>
            {
                if (!_internetRequired)
                    InternetRequiredOverlay.Visibility = Visibility.Collapsed;
            };
            InternetRequiredOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            InternetRequiredText?.BeginAnimation(UIElement.OpacityProperty, null);
            if (InternetRequiredText != null)
                InternetRequiredText.Opacity = 1.0;
        }
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
