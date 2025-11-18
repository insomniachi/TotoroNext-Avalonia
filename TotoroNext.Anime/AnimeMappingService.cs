using System.Text.Json;
using Flurl.Http;
using Microsoft.Data.Sqlite;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using ZstdSharp;

namespace TotoroNext.Anime;

public class AnimeMappingService : IAnimeMappingService
{
    public AnimeId? GetId(AnimeModel anime)
    {
        var column = anime.ServiceName switch
        {
            "MyAnimeList" => "MyAnimeList",
            "Anilist" => "Anilist",
            "AniDb" => "AniDb",
            "Kitsu" => "Kitsu",
            "Simkl" => "Simkl",
            "NotifyMoe" => "NotifyMoe",
            _ => throw new ArgumentException("Invalid service name")
        };

        var db = ModuleHelper.GetFilePath(null, "anime.db");
        using var connection = new SqliteConnection(@$"Data Source={db}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM AnimeMapping WHERE {column} = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", anime.Id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var malId = reader.GetInt32(0);
        var anilistId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
        var aniDbId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
        var kitsuId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
        var simklId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
        var notifyMoe = reader.IsDBNull(5) ? "" : reader.GetString(5);

        return new AnimeId
        {
            MyAnimelist = malId,
            Anilist = anilistId,
            AniDb = aniDbId,
            Kitsu = kitsuId,
            Simkl = simklId,
            NotifyMoe = notifyMoe
        };
    }

    public async Task Update()
    {
        var stream = await "https://api.github.com/repos/manami-project/anime-offline-database/releases/latest"
                           .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                           .GetStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var asset = root.GetProperty("assets")
                        .EnumerateArray()
                        .FirstOrDefault(x => x.GetProperty("name").GetString() == @"anime-offline-database.jsonl.zst");
        var url = asset.GetProperty("browser_download_url").GetString();
        var dbStream = await url.GetStreamAsync();
        UpdateDb(dbStream);
    }

    private static void UpdateDb(Stream stream)
    {
        var db = ModuleHelper.GetFilePath(null, "anime.db");
        using var connection = new SqliteConnection(@$"Data Source={db}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
              CREATE TABLE IF NOT EXISTS AnimeMapping (
                  MyAnimeList INTEGER UNIQUE,  
                  Anilist INTEGER,
                  AniDb INTEGER,
                  Kitsu INTEGER,
                  Simkl INTEGER,
                  NotifyMoe TEXT
              );

            CREATE INDEX IF NOT EXISTS idx_anilist   ON AnimeMapping(Anilist);
            CREATE INDEX IF NOT EXISTS idx_anidb     ON AnimeMapping(AniDb);
            CREATE INDEX IF NOT EXISTS idx_kitsu     ON AnimeMapping(Kitsu);
            CREATE INDEX IF NOT EXISTS idx_simkl     ON AnimeMapping(Simkl);
            CREATE INDEX IF NOT EXISTS idx_notifymoe ON AnimeMapping(NotifyMoe);

            """;
        cmd.ExecuteNonQuery();

        using var transaction = connection.BeginTransaction();

        var insertCmd = connection.CreateCommand();
        insertCmd.CommandText =
            """
              INSERT INTO AnimeMapping (MyAnimeList, Anilist, AniDb, Kitsu, Simkl, NotifyMoe)
              VALUES ($mal, $anilist, $anidb, $kitsu, $simkl, $notifymoe)
              ON CONFLICT(MyAnimeList) DO UPDATE SET
                  Anilist = excluded.Anilist,
                  AniDb = excluded.AniDb,
                  Kitsu = excluded.Kitsu,
                  Simkl = excluded.Simkl,
                  NotifyMoe = excluded.NotifyMoe
            """;

        insertCmd.Parameters.Add("$mal", SqliteType.Integer);
        insertCmd.Parameters.Add("$anilist", SqliteType.Integer);
        insertCmd.Parameters.Add("$anidb", SqliteType.Integer);
        insertCmd.Parameters.Add("$kitsu", SqliteType.Integer);
        insertCmd.Parameters.Add("$simkl", SqliteType.Integer);
        insertCmd.Parameters.Add("$notifymoe", SqliteType.Text);

        foreach (var id in ReadFromOfflineDb(stream))
        {
            BindParams(insertCmd.Parameters, id);
            insertCmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void BindParams(SqliteParameterCollection p, AnimeId id)
    {
        p["$mal"].Value = id.MyAnimelist;
        p["$anilist"].Value = id.Anilist;
        p["$anidb"].Value = id.AniDb;
        p["$kitsu"].Value = id.Kitsu;
        p["$simkl"].Value = id.Simkl;
        p["$notifymoe"].Value = id.NotifyMoe;
    }

    private static IEnumerable<AnimeId> ReadFromOfflineDb(Stream stream)
    {
        using var decompressor = new DecompressionStream(stream);
        using var reader = new StreamReader(decompressor);
        while (reader.ReadLine() is { } line)
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("sources", out var sources))
            {
                continue;
            }

            var id = new AnimeId();

            foreach (var source in sources.EnumerateArray())
            {
                var url = source.GetString();
                if (string.IsNullOrEmpty(url))
                {
                    continue;
                }

                var serviceId = url.Split('/').LastOrDefault();

                if (string.IsNullOrEmpty(serviceId))
                {
                    continue;
                }

                if (url.StartsWith("https://anidb.net/"))
                {
                    id.AniDb = long.Parse(serviceId);
                }
                else if (url.StartsWith("https://anilist.co/"))
                {
                    id.Anilist = long.Parse(serviceId);
                }
                else if (url.StartsWith("https://kitsu.app/"))
                {
                    id.Kitsu = long.Parse(serviceId);
                }
                else if (url.StartsWith("https://myanimelist.net/"))
                {
                    id.MyAnimelist = long.Parse(serviceId);
                }
                else if (url.StartsWith("https://simkl.com/"))
                {
                    id.Simkl = long.Parse(serviceId);
                }
                else if (url.StartsWith("https://notify.moe/"))
                {
                    id.NotifyMoe = serviceId;
                }
            }

            yield return id;
        }
    }
}