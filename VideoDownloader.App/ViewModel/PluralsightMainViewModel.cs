using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Extension;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.ViewModel
{
	/// <summary>
	/// This class contains properties that the main View can data bind to.
	/// <para>
	/// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
	/// </para>
	/// <para>
	/// You can also use Blend to data bind with the tool's support.
	/// </para>
	/// <para>
	/// See http://www.galasoft.ch/mvvm
	/// </para>
	/// </summary>
	public class PluralsightMainViewModel : ViewModelBase
	{
		#region Fields

		private CancellationTokenSource _cts;
		private ObservableCollection<KeyValuePair<string, List<Result>>> _resultsByTagsPairsFilteredList;
		private Dictionary<string, List<Result>> _resultsByTags;
		private List<Result> _allResults;
		private IEnumerable<Result> _currentDisplayedCourses;
		private ObservableCollection<Result> _currentDisplayedFilteredCourses;
		private readonly IConfigProvider _configProvider;
		private bool _isDownloading;
		private bool _anyCourseSelected;
		private string _downloadingCourse;
		private string _currentAction;
		private int _downloadingProgress;
		private int _currentTimeout;
		private bool _canDownload;
		private int _numberOfSelectedCourses;
		private string _tagsFilterText;
		private string _coursesFilterText;
		private string _title;
		private string _currentUserAgent;

		#endregion

		#region Properties

		public bool CanDownload
		{
			get
			{
				return _canDownload;
			}
			set
			{
				Set(() => CanDownload, ref _canDownload, value);
				DownloadCourseCommand.RaiseCanExecuteChanged();
				CancelDownloadsCommand.RaiseCanExecuteChanged();
			}
		}

		public bool IsDownloading
		{
			get
			{
				return _isDownloading;
			}
			set
			{
				Set(() => IsDownloading, ref _isDownloading, value);
				DownloadCourseCommand.RaiseCanExecuteChanged();
				CancelDownloadsCommand.RaiseCanExecuteChanged();
			}
		}

		public int CurrentTimeout
		{
			get
			{
				return _currentTimeout;
			}
			set
			{
				Set(() => CurrentTimeout, ref _currentTimeout, value);
			}
		}

		public string DownloadingCourse
		{
			get
			{
				return _downloadingCourse;
			}
			set
			{
				Set(() => DownloadingCourse, ref _downloadingCourse, value);
			}

		}

		public string CurrentAction
		{
			get
			{
				return _currentAction;
			}
			set
			{
				Set(() => CurrentAction, ref _currentAction, value);
			}

		}

		
		public int DownloadingProgress
		{
			get
			{
				return _downloadingProgress;
			}
			set
			{
				Set(() => DownloadingProgress, ref _downloadingProgress, value);
			}
		}
		
		public bool AnyCourseSelected
		{
			get { return _anyCourseSelected; }
			set
			{
				Set(() => AnyCourseSelected, ref _anyCourseSelected, value);
			}
		}

		public int NumberOfSelectedCourses
		{
			get { return _numberOfSelectedCourses; }
			set
			{
				Set(() => NumberOfSelectedCourses, ref _numberOfSelectedCourses, value);
			}
		}

		public string Title
		{
			get { return _title; }
			set
			{
				Set(() => Title, ref _title, value);
			}
		}

		public string CurrentUserAgent
		{
			get { return _currentUserAgent?? "Undefined" ; }
			set
			{
				Set(() => CurrentUserAgent, ref _currentUserAgent, value);
			}
		}

		public string TagsFilterText
		{
			get { return _tagsFilterText ?? string.Empty; }
			set
			{
				if(Set(() => TagsFilterText, ref _tagsFilterText, value))
				{
					ResultsByTagsPairsFilteredList = 
						new ObservableCollection<KeyValuePair<string, List<Result>>>(ResultsByTags.Where(tag => tag.Key.ToLower().Contains(TagsFilterText.ToLower())).OrderBy(tag => tag.Key));
				}
			}
		}

		public string CoursesFilterText
		{
			get { return _coursesFilterText ?? ""; }
			set
			{
				if (Set(() => CoursesFilterText, ref _coursesFilterText, value))
				{
					if (AllResults.Any())
					{
						CurrentDisplayedFilteredCourses =
							new ObservableCollection<Result>(AllResults.Where(course => course.Title.ToLower().Contains(CoursesFilterText.ToLower())).OrderByDescending(c => c.PublishedDate));
					}
				}
			}
		}

		public ObservableCollection<KeyValuePair<string, List<Result>>> ResultsByTagsPairsFilteredList
		{
			get { return _resultsByTagsPairsFilteredList; }
			set
			{
				Set(() => ResultsByTagsPairsFilteredList, ref _resultsByTagsPairsFilteredList, value);
			}
		}

		public List<Result> AllResults
		{
			get { return _allResults; }
			set
			{
				Set(() => AllResults, ref _allResults, value);
			}
		}

		public Dictionary<string, List<Result>> ResultsByTags
		{
			get { return _resultsByTags; }
			set
			{
				Set(() => ResultsByTags, ref _resultsByTags, value);
			}
		}

		public IEnumerable<Result> CurrentDisplayedCourses
		{
			get { return _currentDisplayedCourses; }
			set
			{
				Set(() => CurrentDisplayedCourses, ref _currentDisplayedCourses, value);
			}
		}

		public ObservableCollection<Result> CurrentDisplayedFilteredCourses
		{
			get { return _currentDisplayedFilteredCourses; }
			set
			{
				Set(() => CurrentDisplayedFilteredCourses, ref _currentDisplayedFilteredCourses, value);
			}
		}

		public ICourseService CourseService { get; }

		#endregion

		#region Command

		public RelayCommand<string> CourseTagSelectedCommand { get; }

		public RelayCommand OpenDownloadsFolderCommand { get; }

		public RelayCommand OpenSettingsWindowCommand { get; }

		public RelayCommand<bool> ProductCheckBoxToggledCommand { get; }

		public RelayCommand DownloadCourseCommand { get; set; }

		public RelayCommand CancelDownloadsCommand { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public PluralsightMainViewModel(ICourseService courseService, IConfigProvider configProvider)
		{
			_configProvider = configProvider;
			CurrentUserAgent = _configProvider.UserAgent;
			CourseService = courseService;
			NumberOfSelectedCourses = 0;
			AuthenticatedUser authenticatedUser = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthenticatedUser>(CourseService.LoginResultJson);
			Title = $"{authenticatedUser.CurrentUser.FirstName} {authenticatedUser.CurrentUser.LastName} ({authenticatedUser.CurrentUser.Email})";

			CourseTagSelectedCommand = new RelayCommand<string>(OnCourseTagSelected);
			CancelDownloadsCommand = new RelayCommand(OnCancelDownloads, CanCancelDownload);
			OpenDownloadsFolderCommand = new RelayCommand(OnOpenDownloadsFolder);
			OpenSettingsWindowCommand = new RelayCommand(OnOpenSettingsWindow);
			ProductCheckBoxToggledCommand = new RelayCommand<bool>(OnProductCheckBoxToggledCommand);
			DownloadCourseCommand = new RelayCommand(OnDownloadCourseAsync, CanExecuteDownload);

			var allProducts = Newtonsoft.Json.JsonConvert.DeserializeObject<AllProducts>(courseService.CachedProductsJson);
			ResultsByTags = new Dictionary<string, List<Result>>();
			AllResults = new List<Result>();
			foreach (var res in allProducts.ResultSets[0].Results)
			{
				AllResults.Add(res);
				string toolsString = res.Tools ?? "not specified";
				var tools = toolsString.Split('|');
				foreach (var t in tools)
				{
					
					ResultsByTags.GetOrCreate(t).Add(res);
				}
			}
			ResultsByTagsPairsFilteredList =
				new ObservableCollection<KeyValuePair<string, List<Result>>>(ResultsByTags.Where(tag => tag.Key.ToLower().Contains(TagsFilterText.ToLower())).OrderBy(tag => tag.Key));
		}

		#endregion

		#region Command executors

		private void OnOpenDownloadsFolder()
		{
			Process.Start(_configProvider.DownloadsPath);
		}

		private void OnOpenSettingsWindow()
		{
			Messenger.Default.Send(new NotificationMessage("OpenSettingsWindow"));
		}

		private bool CanExecuteDownload()
		{
			return !IsDownloading && AnyCourseSelected;
		}

		private bool CanCancelDownload()
		{
			return IsDownloading;
		}

		private void OnProductCheckBoxToggledCommand(bool isChecked)
		{
			if (isChecked)
			{
				++NumberOfSelectedCourses;
			}
			else
			{
				--NumberOfSelectedCourses;
			}
			AnyCourseSelected = (NumberOfSelectedCourses > 0);
			RaisePropertyChanged(() => AnyCourseSelected);
		}

		private async void OnDownloadCourseAsync()
		{
			IsDownloading = true;
			_cts = new CancellationTokenSource();

			var downloadingProgress = new Progress<CourseDownloadingProgressArguments>();
			downloadingProgress.ProgressChanged += (s, e) =>
			{
				DownloadingProgress = e.ClipProgress;
				CurrentAction = e.CurrentAction;
				DownloadingCourse = e.ClipName;
			};

			var timeoutProgress = new Progress<int>();
			timeoutProgress.ProgressChanged += (s, e) =>
			{
				CurrentTimeout = e;
			};

			var coursesToDownload = CurrentDisplayedFilteredCourses.Where(c => c.CheckedForDownloading);
			try
			{
				foreach (var course in coursesToDownload)
				{
					await CourseService.DownloadAsync(course.ProdId, downloadingProgress, timeoutProgress, _cts.Token);
				}
			}
			catch (Exception e)
			{
				throw;
			}
			IsDownloading = false;
		}

		private void OnCourseTagSelected(string tag)
		{
			CurrentDisplayedCourses = ResultsByTags[tag];
			if (CurrentDisplayedCourses.Any())
			{
				CurrentDisplayedFilteredCourses =
					new ObservableCollection<Result>(CurrentDisplayedCourses.Where(course => course.Title.ToLower().Contains(CoursesFilterText.ToLower())).OrderByDescending(c => c.PublishedDate));
			}
			
			AnyCourseSelected = (NumberOfSelectedCourses > 0);
		}

		void OnCancelDownloads()
		{
			_cts?.Cancel();
			IsDownloading = false;
		}

		#endregion
	}
}