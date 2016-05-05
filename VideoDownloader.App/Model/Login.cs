using GalaSoft.MvvmLight;

namespace VideoDownloader.App.Model
{
	internal class Login : ObservableObject
	{
		private string _userName;
		public string UserName
		{
			get { return _userName; }
			set
			{
				Set(() => UserName, ref _userName, value);
			}
		}

		private string _password;
		public string Password
		{
			get { return _password; }
			set
			{
				Set(() => Password, ref _password, value);
			}
		}
	}
}
