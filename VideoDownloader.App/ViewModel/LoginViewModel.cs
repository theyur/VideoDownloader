using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.ViewModel
{
	public class LoginViewModel: ViewModelBase, IDataErrorInfo
	{
		#region Fields

		private readonly Login _login;
		private bool _loginButtonEnabled;
		private bool _showAnimation;
		private bool _firstUserNameCheck = true;
		private string _currentOperation = string.Empty;

		public ICourseService CourseService { get; private set; }
		#endregion

		#region Constructors

		public LoginViewModel(ICourseService сourseService)
		{
			CourseService = сourseService;
			_login = new Login();
			LoginButtonEnabled = false;
			LoginInProgress = false;
			LoginCommand = new RelayCommand<object>(obj => OnLogin(obj));
			CloseCommand = new RelayCommand<ICloseable>(CloseWindow);
		}
		#endregion

		#region Properties

		/// <summary>
		/// Login command
		/// </summary>
		public RelayCommand<object> LoginCommand { get; private set; }

		/// <summary>
		/// Close login form commmadnd
		/// </summary>
		public RelayCommand<ICloseable> CloseCommand { get; private set; }

		public string UserName
		{
			get
			{
                return _login.UserName;
			}

			set
			{
				_login.UserName = value;
				LoginButtonEnabled = ValidateUserName() && !LoginInProgress;
			}
		}

		public string Password
		{
			get
			{
				return _login.Password;
			}

			set
			{
				_login.Password = value;
			}
		}

		public string CurrentOperation
		{
			get { return _currentOperation; }
			set
			{
				Set(() => CurrentOperation, ref _currentOperation, value);
			}
		}

		public bool LoginButtonEnabled
		{
			get { return _loginButtonEnabled; }
			set
			{
				Set(() => LoginButtonEnabled, ref _loginButtonEnabled, value);
			}
		}

		public bool ShowAnimation
		{
			get { return _showAnimation; }
			set
			{
				Set(() => ShowAnimation, ref _showAnimation, value);
			}
		}

		public bool LoginInProgress
		{ get; private set; }

		#endregion

		#region commands

		#endregion

		#region command executors

		private async Task OnLogin(object passwordControl)
		{
			LoginButtonEnabled = false;
			LoginInProgress = true;
			System.Windows.Controls.PasswordBox pwBoxControl = passwordControl as System.Windows.Controls.PasswordBox;
			Debug.Assert(pwBoxControl != null);
			var password = pwBoxControl.Password;
			CurrentOperation = "Trying to login, wait please...";
			LoginResult loginResult = await CourseService.LoginAsync(UserName, password);

			if (loginResult.Status == LoginStatus.LoggedIn)
			{
				CurrentOperation = "Gathering products...";
				bool received = await CourseService.GetProductsJsonAsync();
				if (received)
				{
					Messenger.Default.Send(new NotificationMessage("CloseWindow"));
				}
				else
				{
					CurrentOperation = "Unable to receive products. Try later";
					LoginInProgress = false;
					LoginButtonEnabled = true;
				}
			}
			else
			{
				CurrentOperation = "Login failed";
				LoginInProgress = false;
				LoginButtonEnabled = true;
			}
		}
		 
		private void CloseWindow(ICloseable window)
		{
			window?.Close();
		}

		private bool Login_CanExecute(object o)
		{
			return LoginButtonEnabled && ValidateUserName() && ValidatePassword();
		}

		void OnLogout()
		{

		}

		#endregion

		#region Auxiliary methods

		bool ValidateUserName()
		{
			return !string.IsNullOrEmpty(UserName);
		}

		bool ValidatePassword()
		{
			return !string.IsNullOrEmpty(Password);
		}

		#endregion

		#region IDataError members

		public string this[string columnName]
		{
			get
			{
				string msg = null;
				switch (columnName)
				{
					case "UserName":
						{
							if (!_firstUserNameCheck && !ValidateUserName())
								msg = "Name can't be empty.";
							else
							{
								_firstUserNameCheck = false;
							}
							break;
						}

					//case "Password":
					//	{
					//		if (!_firstUserNameCheck && !ValidatePassword())
					//			msg = "Name can't be empty.";
					//		else
					//		{
					//			_firstUserNameCheck = false;
					//		}
					//		break;
					//	}
				}
				return msg;
			}
		}

		public string Error
		{
			get { return String.Empty; }
		}

		#endregion
	}
}
