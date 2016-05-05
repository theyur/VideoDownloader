using System;

namespace VideoDownloader.App.Model
{
	public class Course
	{
		public string Id { get; set; }

		public DateTime PublishedOn { get; set; }

		public DateTime UpdatedOn { get; set; }

		public string Title { get; set; }

		public string ShortDescription { get; set; }

		public string Description { get; set; }

		public bool UserMayViewFirstClip { get; set; }

		public string Level { get; set; }

		public string Duration { get; set; }

		public string PlayerUrl { get; set; }

		public int PopularityScore { get; set; }

		public Author[] Authors { get; set; }

		public Module[] Modules { get; set; }

		public object Retired { get; set; }

		public object Rating { get; set; }

		public object Audience { get; set; }

		public bool HasTranscript { get; set; }

		public bool HasAssessment { get; set; }

		public object CourseImage { get; set; }

	}
}
