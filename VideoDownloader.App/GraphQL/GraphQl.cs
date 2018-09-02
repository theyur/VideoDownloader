using VideoDownloader.App.Model;

namespace VideoDownloader.App.GraphQL
{
    public static class GraphQl
    {
        public static string GetClipsRequest(CourseRpc course, Module module, int clipIndex) =>
            $"{{\"query\": " +
            $"\"query viewClip {{" +
            $"viewClip (input: " +
            $"{{author: \\\"{course.GetAuthorNameId(module.AuthorId)}\\\", " +
            $"clipIndex: {clipIndex}, " +
            $"courseName: \\\"{course.Name}\\\", " +
            $"includeCaptions: {course.CourseHasCaptions.ToString().ToLowerInvariant()}, locale: \\\"en\\\", " +
            $"mediaType: \\\"mp4\\\", " +
            $"moduleName: \\\"{module.Name}\\\", " +
            $"quality: \\\"{(course.SupportsWideScreenVideoFormats ? Properties.Settings.Default.Resolution1280x720 : Properties.Settings.Default.Resolution1024x768)}\\\"}}) " +
            $"{{urls {{url cdn rank source}}, status}}}}\", " +
            $"\"variables\": {{}}}}";


        public static string GetCourseExtraInfoRequest(string courseId) =>
            $"{{\"query\": " +
            $"\"query BootstrapPlayer {{" +
            $"rpc {{" +
            $"bootstrapPlayer {{" +
            $"extraInfo: " +
            $"course(courseId: \\\"{courseId}\\\") " +
            $"{{courseHasCaptions supportsWideScreenVideoFormats }}" +
            $"}}" +
            $"}}" +
            $"}}\", " +
            $"\"variables\": {{}}}}";
    }
}