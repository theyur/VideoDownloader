using System;
using System.Windows;

namespace VideoDownloader.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var loginView = new LoginWindow();
                loginView.Show();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
