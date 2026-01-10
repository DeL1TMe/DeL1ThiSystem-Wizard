using System;
using System.Windows;
using System.Windows.Controls;

namespace DeL1ThiSystem.ConfigurationWizard.Pages
{
    public partial class DisclaimerPage : Page
    {
        public DisclaimerPage()
        {
            InitializeComponent();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService?.Navigate(new MainPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть экран выбора твиков.\n\n{ex.Message}",
                    "DeL1ThiSystem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
