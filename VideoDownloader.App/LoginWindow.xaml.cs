using GalaSoft.MvvmLight.Messaging;
using VideoDownloader.App.Contracts;
using VideoDownloader.App.ViewModel;

namespace VideoDownloader.App
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : ICloseable
    {
        public LoginWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
            MouseLeftButtonDown += delegate {DragMove(); };
            Messenger.Default.Register<NotificationMessage>(this, (message) =>
            {
                switch (message.Notification)
                {
                    case "CloseWindow":
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        Close();
                        break;
                }
            });
        }
    }
}
