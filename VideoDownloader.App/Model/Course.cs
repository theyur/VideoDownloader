using System;

namespace VideoDownloader.App.Model
{
	public class Course
	{
		public object Audiences { get; set; }

		public Author[] Authors { get; set; }
		public object CourseImage { get; set; }
		public string Description { get; set; }
		public string Duration { get; set; }
		public bool HasTranscript { get; set; }
		public bool HasMentorship { get; set; }

		public string Id { get; set; }
		public string Level { get; set; }
		public Module[] Modules { get; set; }
		public string PlayerUrl { get; set; }

		public int PopularityScore { get; set; }
		public DateTime PublishedOn { get; set; }
		public object Rating { get; set; }
		public object Retired { get; set; }
		public string ShortDescription { get; set; }
		public object SkillPaths { get; set; }
		public string Title { get; set; }
		public DateTime UpdatedOn { get; set; }
	}
}
