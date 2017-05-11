using System;

namespace VideoDownloader.App.Model
{
	public class Author
	{
		public string Id { get; set; }
		public string DisplayName { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}

	public class Result
	{
		public string ProdId { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string KeyWords { get; set; }

		public string Url { get; set; }

		public string Loc { get; set; }

		public string Categories { get; set; }

		public string CourseName { get; set; }

		public string Duration { get; set; }

		public int ImageVersion { get; set; }

		public string Subjects { get; set; }

		public string SkillLevels { get; set; }

		public string Tools { get; set; }

		public string Certifications { get; set; }


		public DateTime PublishedDate { get; set; }

		public DateTime UpdatedDate { get; set; }

		public double AverageRating { get; set; }

		// here is is bool, but in product json it is object with 3 fields
		public bool Retired { get; set; }

		public bool HasTranscript { get; set; }

		public Author[] Authors { get; set; }

		public string Last { get; set; }

		public bool CheckedForDownloading { get; set; }

	}

	public class ResultSet
	{
		public string Name { get; set; }
		public CourseDescription[] Results { get; set; }
	}
	public class AllProducts
	{
		public dynamic General { get; set; }
		public dynamic Banners { get; set; }
		public dynamic Menus { get; set; }
		public dynamic BreadCrumbs { get; set; }
		public dynamic Pagination { get; set; }
		public dynamic Facets { get; set; }
		public ResultSet[] ResultSets { get; set; }
		public dynamic ResultCount { get; set; }

	}
}
