namespace VideoDownloader.App.Model
{
	public class Module
	{
		public string Id { get; set; }

		public string Title { get; set; }

		public string Duration { get; set; }

		public string PlayerUrl { get; set; }

		public Clip[] Clips { get; set; }
	}
}
