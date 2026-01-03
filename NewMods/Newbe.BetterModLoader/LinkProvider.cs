using System.Net;

namespace Newbe.BetterModLoader;

internal class LinkProvider
{
    public static LinkProvider Instance { get; } = new();

    private Network _network = Network.Unknown;

    public string GetSampleRepoLink()
    {
        return _network switch
        {
            Network.Cn => CK.Links.DuckySampleGiteeRepo,
            _ => CK.Links.DuckySamplesGithubRepo,
        };
    }

    public string GetLogEfficientModLink()
    {
        return CK.Links.LogEfficientModSteamWorkshopPage;
    }

    public string GetFpsMonitorModLink()
    {
        return CK.Links.FpsMonitorModSteamWorkshopPage;
    }

    public UniTask EnsureNetworkDeterminedAsync()
    {
        if (_network != Network.Unknown)
        {
            return UniTask.CompletedTask;
        }

        return UniTask.RunOnThreadPool(() =>
        {
            try
            {
                using var webClient = new MyWebClient();
                webClient.DownloadString("https://github.com/");
                webClient.DownloadString("https://www.google.com.hk/");

                _network = Network.Global;
            }
            catch
            {
                _network = Network.Cn;
            }
        });
    }

    private enum Network
    {
        Unknown,
        Cn,
        Global
    }

    private class MyWebClient : WebClient
    {
        private static int _defaultTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var w = base.GetWebRequest(uri);
            w.Timeout = _defaultTimeout;
            return w;
        }
    }
}
