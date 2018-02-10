using System.Threading.Tasks;

namespace VideoDownloader.App.Contracts
{
    public interface ILoginService
    {
        Task<LoginResult> LoginAsync(string userName, string password);

        string LoginResultJson { get; set; }

    }
}