using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    public class PluralsightLoginService:ILoginService
    {
        private string _cookies;

        public string LoginResultJson { get; set; }

        public async Task<LoginResult> LoginAsync(string userName, string password)
        {
            var postData = BuildPostDataString(userName, password);
            var loginResult = new LoginResult() { DataJson = string.Empty, Status = LoginStatus.Failed };

            ResponseEx loginResponse = null;
            HttpMethod httpMethod = HttpMethod.Post;
            Uri urlToGo = new Uri("https://app.pluralsight.com/id/");
            do
            {
                loginResponse = await HttpHelper.SendRequest(httpMethod,
                    urlToGo,
                    postData, AcceptHeader.HtmlXml,
                    ContentType.AppXWwwFormUrlencode,
                    new Uri("https://app.pluralsight.com/id/"),
                    _cookies);
                _cookies += $"{loginResponse.Cookies};";
                if (loginResponse.RedirectUrl != null)
                {
                    urlToGo = new Uri(loginResponse.RedirectUrl);
                }
                httpMethod = HttpMethod.Get;
                ;
            } while (loginResponse.ResponseMessage.StatusCode == HttpStatusCode.Redirect);
            string userData;
            if (IsLoggedIn(loginResponse.Content, out userData))
            {
                loginResult.Status = LoginStatus.LoggedIn;
                loginResult.DataJson = userData;
                LoginResultJson = loginResult.DataJson;
                loginResult.Cookies = _cookies;
            }
            else
            {
                loginResult.Status = LoginStatus.Failed;
            }
            return loginResult;
        }

        private string BuildPostDataString(string userName, string password)
        {
            return
                $"RedirectUrl=&Username={userName}" +
                $"&Password={password}" +
                "&ShowCaptcha=False" +
                "&ReCaptchaSiteKey=6LeVIgoTAAAAAIhx_TOwDWIXecbvzcWyjQDbXsaV";
        }

        bool IsLoggedIn(string content, out string loggedInUserData)
        {
            bool loggedIn = false;
            loggedInUserData = string.Empty;

            if (!string.IsNullOrEmpty(content))
            {
                var loginDoneRegExp = new Regex("{\"currentUser\":{.+\\:.+}");
                var loginDoneMatch = loginDoneRegExp.Match(content);
                if (loginDoneMatch.Success)
                {
                    loggedInUserData = loginDoneMatch.Groups[0].Value;
                    loggedIn = true;
                }
            }
            return loggedIn;
        }
    }
}