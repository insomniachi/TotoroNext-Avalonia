using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Flurl;
using Flurl.Http;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Torrents.Nyaa;

public class Indexer : ITorrentIndexer
{
    public async IAsyncEnumerable<TorrentModel> SearchAsync(string term, string group, string quality)
    {
        var sb = new StringBuilder(term);
        if (!string.IsNullOrEmpty(group))
        {
            sb.Append($" {group}");
        }

        if (!string.IsNullOrEmpty(quality))
        {
            sb.Append($" {quality}");
        }

        var query = sb.ToString();
        var stream = await "https://nyaa.si/"
                           .AppendQueryParam("page", "rss")
                           .AppendQueryParam("f", 2)
                           .AppendQueryParam("c", "1_2")
                           .AppendQueryParam("q", query)
                           .GetStreamAsync();

        using var reader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(reader);

        foreach (var item in feed.Items)
        {
            if (TorrentModel.Create(item.Links[0].Uri, item.Title.Text) is not { } torrent)
            {
                continue;
            }

            foreach (var field in item.ElementExtensions)
            {
                switch (field.OuterName)
                {
                    case "seeders":
                        torrent.Seeders = field.GetObject<int>();
                        break;
                    case "leechers":
                        torrent.Leechers = field.GetObject<int>();
                        break;
                    case "downloads":
                        torrent.Downloads = field.GetObject<int>();
                        break;
                    case "infoHash":
                        torrent.InfoHash = field.GetObject<string>();
                        break;
                    case "size":
                        torrent.Size = field.GetObject<string>();
                        break;
                }
            }

            yield return torrent;
        }
    }
}