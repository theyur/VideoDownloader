using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VideoDownloader.App.Contract;
using VideoDownloader.App.ViewModel;

namespace VideoDownloader.App
{
	/// <summary>
	/// Interaction logic for LoginWindow.xaml
	/// </summary>
	public partial class LoginWindow : Window, ICloseable
	{
		public LoginWindow()
		{
			InitializeComponent();
			Closing += (s, e) => ViewModelLocator.Cleanup();
			this.MouseLeftButtonDown += delegate { this.DragMove(); };
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
