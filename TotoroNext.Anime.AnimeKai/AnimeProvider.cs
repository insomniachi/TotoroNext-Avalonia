﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.AnimeKai;

public class AnimeProvider(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    private readonly MegaUpExtractor _extractor = new MegaUpExtractor();
    
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        using var client = CreateClient();

        var stream = await client.Request("browser")
                                 .AppendQueryParam("keyword", query)
                                 .AppendQueryParam("page", 1)
                                 .GetStreamAsync();

        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var item in doc.QuerySelectorAll(".aitem") ?? [])
        {
            var title = item.QuerySelector(".title").GetAttributeValue("title", string.Empty);
            var id = item.QuerySelector(".ttip-btn").GetAttributeValue("data-tip", string.Empty);
            var image = item.QuerySelector(".lazyload").GetAttributeValue("data-src", string.Empty);

            yield return new SearchResult(this, id, title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        using var client = CreateClient();
        var enc = await EncodeDecodeEndpoint(episodeId);

        var response = await client.Request("ajax/links/list")
                                   .AppendQueryParam("token", episodeId)
                                   .AppendQueryParam("_", enc.Result)
                                   .GetJsonAsync<ResultResponse<string>>();


        var doc = new HtmlDocument();
        doc.LoadHtml(response.Result!);

        foreach (var category in doc.QuerySelectorAll(".server-items") ?? [])
        {
            var type = category.GetAttributeValue("data-id", string.Empty);
            
            foreach (var server in category.QuerySelectorAll(".server"))
            {
                var id = server.GetAttributeValue("data-lid", string.Empty);
                var name = server.InnerText;
                var iframeResponse = await ExtractIFrame(client, id);
                if (iframeResponse is null)
                {
                    continue;
                }

                yield return new VideoServer($"{name} ({type})", new Uri(iframeResponse.Url), _extractor)
                {
                    SkipData = ConvertSkipData(iframeResponse.SkipData)
                };
            }
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        using var client = CreateClient();
        var enc = await EncodeDecodeEndpoint(animeId);

        var response = await client.Request("ajax/episodes/list")
                                   .AppendQueryParam("ani_id", animeId)
                                   .AppendQueryParam("_", enc.Result)
                                   .GetJsonAsync<ResultResponse<string>>();

        var doc = new HtmlDocument();
        doc.LoadHtml(response.Result!);

        foreach (var item in doc.QuerySelectorAll(".eplist a") ?? [])
        {
            var number = item.GetAttributeValue("num", string.Empty);
            var token = item.GetAttributeValue("token", string.Empty);
            var title = item.InnerText;

            yield return new Episode(this, animeId, token, float.Parse(number))
            {
                Info = new EpisodeInfo
                {
                    Titles =
                    {
                        English = title
                    }
                }
            };
        }
    }

    private static async Task<IFrameResponse?> ExtractIFrame(FlurlClient client, string id)
    {
        var enc = await EncodeDecodeEndpoint(id);
        var encodedLink = await client.Request("ajax/links/view")
                                      .AppendQueryParam("id", id)
                                      .AppendQueryParam("_", enc.Result)
                                      .GetJsonAsync<ResultResponse<string>>();
        
        var response = await "https://enc-dec.app/api/dec-kai"
              .PostJsonAsync(new
              {
                  text = encodedLink.Result
              }).ReceiveJson<ResultResponse<IFrameResponse>>();

        return response.Result;
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient(typeof(Module).FullName!));
    }

    private static Task<ResultResponse<string>> EncodeDecodeEndpoint(string encoded)
    {
        return $"https://enc-dec.app/api/enc-kai?text={encoded}".GetJsonAsync<ResultResponse<string>>();
    }
    
    private static Abstractions.Models.SkipData? ConvertSkipData(SkipData? skipData)
    {
        if (skipData is null)
        {
            return null;
        }
        
        return new Abstractions.Models.SkipData
        {
            Opening = skipData.Intro.Length == 2
                ? new Segment
                {
                    Start = TimeSpan.FromSeconds(skipData.Intro[0]),
                    End = TimeSpan.FromSeconds(skipData.Intro[1])
                }
                : null,
            Ending = skipData.Outro.Length == 2
                ? new Segment
                {
                    Start = TimeSpan.FromSeconds(skipData.Outro[0]),
                    End = TimeSpan.FromSeconds(skipData.Outro[1])
                }
                : null
        };
    }
}

[Serializable]
internal class ResultResponse<T>
{
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("result")] public T? Result { get; set; }
}

[Serializable]
internal class IFrameResponse
{
    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    [JsonPropertyName("skip")] public SkipData? SkipData { get; set; }
}

[Serializable]
internal class SkipData
{
    [JsonPropertyName("intro")] public int[] Intro { get; set; } = [];
    [JsonPropertyName("outro")] public int[] Outro { get; set; } = [];
}