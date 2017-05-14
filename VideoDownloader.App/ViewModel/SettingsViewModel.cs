using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows.Forms;
using VideoDownloader.App.Contract;

namespace VideoDownloader.App.ViewModel
{
	public class SettingsViewModel : ViewModelBase
	{
		#region Fields

	    int _minTimeout;
		int _maxTimeout;
		string _downloadsPath;

		#endregion

		#region Properties

		public RelayCommand OpenSelectFolderDialogCommand { get; set; }

		public RelayCommand SaveSettingsCommand { get; set; }

		public RelayCommand CancelCommand { get; set; }


		public int MinTimeout
		{
			get { return _minTimeout; }
			set
			{
				Set(() => MinTimeout, ref _minTimeout, value);
			}
		}

		public int MaxTimeout
		{
			get { return _maxTimeout; }
			set
			{
				Set(() => MaxTimeout, ref _maxTimeout, value);
			}
		}

		public string DownloadsPath
		{
			get { return _downloadsPath; }
			set
			{
				Set(() => DownloadsPath, ref _downloadsPath, value);
			}
		}

		#endregion

		#region Constructor

		public SettingsViewModel(IConfigProvider configProvider)
		{
		    var configProvider1 = configProvider;

			MinTimeout = configProvider1.MinTimeout;
			MaxTimeout = configProvider1.MaxTimeout;
			DownloadsPath = configProvider1.DownloadsPath;

			OpenSelectFolderDialogCommand = new RelayCommand(() =>
			{
				var dialog = new FolderBrowserDialog();
				var selectedPath = dialog.ShowDialog();
				if (selectedPath == DialogResult.OK)
				{
					DownloadsPath = dialog.SelectedPath;
				}
			});

			SaveSettingsCommand = new RelayCommand(() =>
			{
				configProvider1.MinTimeout = MinTimeout;
				configProvider1.MaxTimeout = MaxTimeout;
				configProvider1.DownloadsPath = DownloadsPath;

				configProvider1.Save();
				try
				{
					Messenger.Default.Send(new NotificationMessage("CloseSettingsWindow"));
				}
			    catch (Exception /*exc*/)
			    {
			        // ignored
			    }
			});

			CancelCommand = new RelayCommand(() =>
			{
				Messenger.Default.Send(new NotificationMessage("CloseSettingsWindow"));
			});
		}
	}
	#endregion
}
