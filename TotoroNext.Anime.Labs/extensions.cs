using Downloader;
using Flurl;
using Flurl.Http;
using TotoroNext.Module;

namespace TotoroNext.Anime.Labs;

internal static class Extensions
{
    extension(Url url)
    {
        internal IFlurlRequest WithRequiredHeaders()
        {
            return url.WithHeader(HttpHeaderNames.Referer, "https://av1encodes.com/")
                      .WithHeader(HttpHeaderNames.UserAgent, Http.UserAgent)
                      .WithHeader(HttpHeaderNames.Accept,
                                  "application/json,text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8")
                      .WithHeader(HttpHeaderNames.AcceptLanguage, "en-US,en;q=0.9")
                      .WithHeader("Sec-Ch-Ua", "\"Chromium\";v=\"124\", \"Google Chrome\";v=\"124\"")
                      .WithHeader("Sec-Ch-Ua-Mobile", "?0")
                      .WithHeader("Sec-Ch-Ua-Platform", "\"Windows\"")
                      .WithHeader("Sec-Fetch-Dest", "empty")
                      .WithHeader("Sec-Fetch-Mode", "cors")
                      .WithHeader("Sec-Fetch-Site", "same-origin")
                      .WithHeader("Priority", "u=1,i");
        }
    }
}