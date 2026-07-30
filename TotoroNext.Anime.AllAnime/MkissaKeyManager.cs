using System.Text.Json;
using System.Text.RegularExpressions;

namespace TotoroNext.Anime.AllAnime;

/// <summary>
///     Owns the "aaReq" key material: `buildId` + mask seeds are scraped from the live JS bundle and
///     fold into the client mask, which signs the bootstrap request for `partB`; the key is
///     `mask XOR partB`.
/// </summary>
public partial class MKissaKeyManager(
    HttpClient client,
    Dictionary<string, string> headers,
    string siteUrl,
    string apiUrl)
{
    // Constants that were implicitly available in the Kotlin file context
    public const string StreamHash = "f4662f4b7510b26795dd53ef824a0bf1740fbbc5d1273fab18222ac831bca8d0";
    private const string AnimeLane = "k7";

    private const string MaterialError = "Unable to obtain MKissa crypto material";
    private const string BootstrapPath = "/client-crypto/v1/bootstrap";
    private const string KeyGroup = "mkissa";
    private const string FieldSeparator = "|";
    private const int MaxBuildChunks = 40;
    private const long MaterialTtlMs = 6 * 60 * 60 * 1000L;
    private const string CryptoChunkMarker = "aaReq";
    private static readonly HashSet<int> StaleCodes = new() { 403, 404 };

    private readonly SemaphoreSlim _materialMutex = new(1, 1);

    private volatile Material? _cachedMaterial;

    // Property to replace the Kotlin SharedPreferences delegate
    private string? StoredBuild { get; set; }

    [GeneratedRegex("""import\("([^"]*/entry/app\.[^"]*\.js)"\)""")]
    private static partial Regex AppEntryRegex();

    [GeneratedRegex("""["'](\.\.?/[\w./-]+\.js)["']""")]
    private static partial Regex ChunkRefRegex();

    public async Task<Material> GetMaterialAsync(bool forceRefresh = false)
    {
        var enteredAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (!forceRefresh)
        {
            if (_cachedMaterial != null && !_cachedMaterial.IsExpired())
            {
                return _cachedMaterial;
            }
        }

        await _materialMutex.WaitAsync();
        try
        {
            if (_cachedMaterial != null)
            {
                if (_cachedMaterial.FetchedAt > enteredAt || (!forceRefresh && !_cachedMaterial.IsExpired()))
                {
                    return _cachedMaterial;
                }
            }

            var handshake = await GetHandshakeAsync() ?? throw new Exception(MaterialError);

            byte[] partB;
            try
            {
                partB = Convert.FromBase64String(handshake.Bootstrap!.PartB);
            }
            catch
            {
                throw new Exception(MaterialError);
            }

            if (partB.Length < 32)
            {
                throw new Exception(MaterialError);
            }

            if (handshake.Build is null)
            {
                throw new Exception(MaterialError);
            }

            // Only after the server accepted it, so a bad parse cannot wedge every later launch.
            StoredBuild = SerializeBuild(handshake.Build!);

            // Not the bootstrap's switchAt: it can already be past while the epoch is live.
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var material = new Material(
                                        MKissaCrypto.DeriveKey(handshake.Mask, partB),
                                        handshake.Bootstrap.Epoch,
                                        handshake.Build.BuildId,
                                        now + MaterialTtlMs,
                                        now
                                       );

            _cachedMaterial = material;
            return material;
        }
        finally
        {
            _materialMutex.Release();
        }
    }

    public static string AaReq(Material material)
    {
        return MKissaCrypto.BuildAaReq(material.Key, material.Epoch, material.BuildId, StreamHash, AnimeLane);
    }

    public static string Decrypt(string toBeParsed, Material material)
    {
        return MKissaCrypto.Decrypt(toBeParsed, material.Key);
    }

    public void Invalidate()
    {
        _cachedMaterial = null;
    }

    /// <summary>
    ///     Used when the streams API rejects a token the bootstrap minted, which it cannot detect.
    /// </summary>
    public void InvalidateBuild()
    {
        StoredBuild = "";
        _cachedMaterial = null;
    }

    public bool IsCryptoError(string body)
    {
        try
        {
            var response = JsonSerializer.Deserialize<AaApiError>(body);
            return response?.Errors?.Any(e => e.Extensions?.Code?.StartsWith("AA_CRYPTO") == true) == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Re-scraping starts at the Cloudflare-gated HTML, so cheaper causes are ruled out first.
    /// </summary>
    private async Task<Handshake?> GetHandshakeAsync()
    {
        var cached = GetCachedBuild();
        var mask = cached != null ? MKissaCrypto.DeriveMask(cached.BuildId, cached.Seeds) : null;

        if (cached != null && mask != null)
        {
            var first = await GetBootstrapAsync(cached.BuildId, mask, MKissaCrypto.EpochCandidates());
            if (first.Bootstrap != null)
            {
                return new Handshake(cached, mask, first.Bootstrap);
            }

            if (!first.Stale)
            {
                return null;
            }

            // A clock off by more than the grace window looks exactly like a stale build.
            var skewed = await GetBootstrapAsync(cached.BuildId, mask, MKissaCrypto.SkewedEpochCandidates());
            if (skewed.Bootstrap != null)
            {
                return new Handshake(cached, mask, skewed.Bootstrap);
            }
        }

        var fresh = await ResolveBuildAsync();
        if (fresh == null)
        {
            return null;
        }

        var freshMask = MKissaCrypto.DeriveMask(fresh.BuildId, fresh.Seeds);
        if (freshMask == null)
        {
            return null;
        }

        var freshResult = await GetBootstrapAsync(fresh.BuildId, freshMask, MKissaCrypto.EpochCandidates());
        return freshResult.Bootstrap != null ? new Handshake(fresh, freshMask, freshResult.Bootstrap) : null;
    }

    private async Task<BootstrapResult> GetBootstrapAsync(string buildId, byte[] mask, IEnumerable<long> epochs)
    {
        var host = new Uri(siteUrl).Host;
        var url = $"{apiUrl.TrimEnd('/')}{BootstrapPath}?buildId={Uri.EscapeDataString(buildId)}&k={Uri.EscapeDataString(AnimeLane)}";

        var sawStale = false;
        foreach (var epoch in epochs)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Add base headers
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Add specific headers
            request.Headers.TryAddWithoutValidation("x-build-id", buildId);
            request.Headers.TryAddWithoutValidation("x-aa-boot", MKissaCrypto.BootToken(mask, buildId, epoch, KeyGroup, host, AnimeLane));
            request.Headers.TryAddWithoutValidation("Origin", siteUrl);
            request.Headers.TryAddWithoutValidation("Referer", $"{siteUrl}/");

            HttpResponseMessage? response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch
            {
                return new BootstrapResult(null, false);
            }

            if (!response.IsSuccessStatusCode)
            {
                if (StaleCodes.Contains((int)response.StatusCode))
                {
                    sawStale = true;
                }

                response.Dispose();
                continue;
            }

            AaCryptoBootstrap? bootstrap;
            try
            {
                var body = await response.Content.ReadAsStringAsync();
                bootstrap = JsonSerializer.Deserialize<AaCryptoBootstrap>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                response.Dispose();
                return new BootstrapResult(null, false);
            }
            finally
            {
                response.Dispose();
            }

            if (bootstrap == null)
            {
                return new BootstrapResult(null, false);
            }

            // A partB from another lane would silently derive the wrong key.
            if (bootstrap.K != null && bootstrap.K != AnimeLane)
            {
                continue;
            }

            return new BootstrapResult(bootstrap, false);
        }

        return new BootstrapResult(null, sawStale);
    }

    private MKissaBundle.BuildInfo? GetCachedBuild()
    {
        if (string.IsNullOrEmpty(StoredBuild))
        {
            return null;
        }

        var separatorIdx = StoredBuild.IndexOf(FieldSeparator, StringComparison.Ordinal);
        if (separatorIdx == -1)
        {
            return null;
        }

        var buildId = StoredBuild[..separatorIdx];
        if (string.IsNullOrEmpty(buildId))
        {
            return null;
        }

        var seedsStr = StoredBuild[(separatorIdx + FieldSeparator.Length)..];
        var seeds = seedsStr.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        return seeds.Count != MKissaCrypto.SeedCount ? null : new MKissaBundle.BuildInfo(buildId, seeds);
    }

    /// <summary>
    ///     The entry is re-read every time: chunk URLs are immutable, so a rebuild only shows in HTML.
    /// </summary>
    private async Task<MKissaBundle.BuildInfo?> ResolveBuildAsync()
    {
        var entryUrlStr = await EntryUrlFromSiteAsync();
        if (entryUrlStr == null)
        {
            return null;
        }

        if (!Uri.TryCreate(entryUrlStr, UriKind.Absolute, out var appUrl))
        {
            return null;
        }

        string appJs;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, appUrl);
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            appJs = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }

        // Shared chunks first: that is where it has always lived.
        var chunkRefs = ChunkRefRegex()
                        .Matches(appJs)
                        .Select(m => m.Groups[1].Value)
                        .Distinct()
                        .OrderByDescending(r => r.Contains("/chunks/"))
                        .Take(MaxBuildChunks)
                        .ToList();

        foreach (var r in chunkRefs)
        {
            if (!Uri.TryCreate(appUrl, r, out var chunkUrl))
            {
                continue;
            }

            string body;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, chunkUrl);
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                using var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                body = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                continue;
            }

            if (!body.Contains(CryptoChunkMarker))
            {
                continue;
            }

            var parsed = MKissaBundle.Parse(body);
            if (parsed != null)
            {
                return parsed;
            }
        }

        return null;
    }

    /// <summary>
    ///     Cloudflare-gated; only needed to locate the CDN app entry.
    /// </summary>
    private async Task<string?> EntryUrlFromSiteAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{siteUrl}/");
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            var match = AppEntryRegex().Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static string SerializeBuild(MKissaBundle.BuildInfo build)
    {
        return $"{build.BuildId}{FieldSeparator}{string.Join(",", build.Seeds)}";
    }

    public class Material(byte[] key, long epoch, string buildId, long expiresAt, long fetchedAt)
    {
        public byte[] Key { get; } = key;
        public long Epoch { get; } = epoch;
        public string BuildId { get; } = buildId;
        public long ExpiresAt { get; } = expiresAt;
        public long FetchedAt { get; } = fetchedAt;

        public bool IsExpired()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= ExpiresAt;
        }
    }

    private class Handshake(MKissaBundle.BuildInfo? build, byte[] mask, AaCryptoBootstrap? bootstrap)
    {
        public MKissaBundle.BuildInfo? Build { get; } = build;
        public byte[] Mask { get; } = mask;
        public AaCryptoBootstrap? Bootstrap { get; } = bootstrap;
    }

    /// <summary>
    ///     [stale] distinguishes "server refused this build" from a network fault.
    /// </summary>
    private class BootstrapResult(AaCryptoBootstrap? bootstrap, bool stale)
    {
        public AaCryptoBootstrap? Bootstrap { get; } = bootstrap;
        public bool Stale { get; } = stale;
    }
}