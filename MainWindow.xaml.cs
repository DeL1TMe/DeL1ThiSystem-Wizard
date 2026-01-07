using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DeL1ThiSystem.ConfigurationWizard.Pages;
using DeL1ThiSystem.ConfigurationWizard.Tweaks;

namespace DeL1ThiSystem.ConfigurationWizard;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadHeaderLogo();
    }

    private void LoadHeaderLogo()
    {
        try
        {
            string assetsRoot = @"C:\ProgramData\DeL1ThiSystem\Wizard\Assets";
            string logo = Path.Combine(assetsRoot, "logo.png");
            if (File.Exists(logo))
                HeaderLogo.Source = new BitmapImage(new Uri(logo, UriKind.Absolute));
        }
        catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;

        // Bootstrap stage (before welcome/disclaimer)
        if (!app.State.BootstrapApplied)
        {
            var boot = TweaksJsonLoader.LoadBootstrapSteps();
            Frame.Navigate(new ProgressPage(boot.Steps, boot.Title, showFooter: false, showReboot: false));
            return;
        }

        NavigateToDisclaimer();
    }

    public void NavigateToDisclaimer() => Frame.Navigate(new DisclaimerPage());

    public void NavigateToMain() => Frame.Navigate(new MainPage());

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
