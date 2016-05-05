namespace VideoDownloader.App.Model
{
	public class UserAvatar
	{
		public string DefaultUrl { get; set; }
	}

	public class Subscription
	{
		public string Type { get; set; }
		public bool HasCodeSchool { get; set; }
		public bool Active { get; set; }
		public bool IsSlice { get; set; }
	}

	public class CurrentUser
	{
		public string Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string DisplayName { get; set; }

		public string Email { get; set; }

		public string UserName { get; set; }

		public UserAvatar Avatar { get; set; }

		public string CreatedAt { get; set; }

		public dynamic[] Roles { get; set; }

		public string[] UserFlags { get; set; }

		public string[] SliceSubscriptions { get; set; }

		public Subscription[] Subscriptions { get; set; }

		public dynamic Config { get; set; }
	}

	public class AuthenticatedUser
	{
		public CurrentUser CurrentUser { get; set; }
	}
}