using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TotoroNext.Anime.AllAnime.Views;

/// <summary>
///     Stateless primitives for the "aaReq" scheme, wire layout `[0x01] + iv(12) + AES-GCM(ct‖tag)`.
///     Request token and response payload are both keyed with `clientMask XOR partB`.
/// </summary>
public static class MKissaCrypto
{
    private const int TagLengthBits = 128;
    private const int TagLengthBytes = TagLengthBits / 8; // 16 bytes
    private const string LegacySecret = "Xot36i3lK3";

    private const int KeySize = 32;
    public const int SeedCount = 4;
    private const int SeedSize = KeySize / SeedCount;

    private const int IvSize = 12;
    private const int HeaderSize = 1 + IvSize;

    private const long WindowMs = 5 * 60 * 1000L;

    // The server derives partB from a 3-day epoch and keeps the previous alive for a day.
    private const long EpochWindowMs = 3 * 24 * 60 * 60 * 1000L;
    private const long EpochGraceMs = 24 * 60 * 60 * 1000L;

    /// <summary>
    ///     `seeds XOR f(buildId) XOR f(position)`; both inputs change on every site rebuild.
    /// </summary>
    public static byte[] DeriveMask(string buildId, List<string> seeds)
    {
        if (string.IsNullOrEmpty(buildId) || seeds.Count != SeedCount)
        {
            return null;
        }

        var stream = new byte[KeySize];
        for (var i = 0; i < KeySize; i++)
        {
            stream[i] = (byte)(buildId[i % buildId.Length] ^ ((i * 17 + 31) & 0xFF));
        }

        var mask = new byte[KeySize];
        for (var index = 0; index < seeds.Count; index++)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(seeds[index]);
            }
            catch (FormatException)
            {
                return null;
            }

            if (bytes.Length < SeedSize)
            {
                return null;
            }

            var @base = index * SeedSize;
            for (var offset = 0; offset < SeedSize; offset++)
            {
                mask[@base + offset] = (byte)(
                    bytes[offset] ^
                    stream[@base + offset] ^
                    ((index * 41 + offset * 7) & 0xFF)
                );
            }
        }

        return mask;
    }

    public static byte[] DeriveKey(byte[] mask, byte[] partB)
    {
        var keyBytes = new byte[KeySize];
        for (var i = 0; i < KeySize; i++)
        {
            keyBytes[i] = (byte)(partB[i] ^ mask[i % mask.Length]);
        }

        return keyBytes;
    }

    /// <summary>
    ///     `x-aa-boot`, checked by the bootstrap endpoint before it hands out `partB`.
    /// </summary>
    public static string BootToken(
        byte[] mask,
        string buildId,
        long epoch,
        string keyGroup,
        string refererHost,
        string lane)
    {
        var inner = Hmac(mask, $"aa-boot:{buildId}");

        var sb = new StringBuilder();
        sb.Append(buildId).Append(':').Append(keyGroup).Append(':')
          .Append(refererHost).Append(':').Append(epoch);

        if (!string.IsNullOrEmpty(lane))
        {
            sb.Append(':').Append(lane);
        }

        return Convert.ToHexString(Hmac(inner, sb.ToString())).ToLowerInvariant();
    }

    /// <summary>
    ///     Oldest first: during the grace window the server still mints `partB` for the previous.
    /// </summary>
    public static List<long> EpochCandidates(long? now = null)
    {
        var currentTime = now ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var current = currentTime / EpochWindowMs;
        var inGrace = currentTime - current * EpochWindowMs < EpochGraceMs && current > 0;
        return inGrace ? new List<long> { current - 1, current } : new List<long> { current };
    }

    /// <summary>
    ///     Fallback for a device clock off by more than the grace window.
    /// </summary>
    public static List<long> SkewedEpochCandidates(long? now = null)
    {
        var currentTime = now ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var current = currentTime / EpochWindowMs;

        var candidates = new List<long> { current + 1, current - 1 }.Where(x => x > 0).ToList();
        var standardCandidates = EpochCandidates(currentTime);

        return candidates.Except(standardCandidates).ToList();
    }

    public static string BuildAaReq(byte[] key, long epoch, string buildId, string queryHash, string lane)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / WindowMs * WindowMs;

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{epoch}:{buildId}:{queryHash}:{ts}:{lane}"));
        var iv = new byte[IvSize];
        Array.Copy(digest, iv, IvSize);

        var payloadObj = new AaReqPayload(1, ts, epoch, buildId, queryHash, lane);
        var payloadStr = JsonSerializer.Serialize(payloadObj);
        var plaintext = Encoding.UTF8.GetBytes(payloadStr);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLengthBytes];

        using (var aesGcm = new AesGcm(key, TagLengthBytes))
        {
            aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
        }

        // Assemble: [0x01] + iv(12) + ciphertext + tag
        var blob = new byte[HeaderSize + ciphertext.Length + tag.Length];
        blob[0] = 1;
        Buffer.BlockCopy(iv, 0, blob, 1, IvSize);
        Buffer.BlockCopy(ciphertext, 0, blob, HeaderSize, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, blob, HeaderSize + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(blob);
    }

    public static string Decrypt(string base64Payload, byte[] materialKey)
    {
        byte[] blob;
        try
        {
            blob = Convert.FromBase64String(base64Payload);
        }
        catch (FormatException)
        {
            return null;
        }

        if (blob.Length < HeaderSize + TagLengthBytes)
        {
            return null;
        }

        int version = blob[0];
        var iv = blob[1..HeaderSize];

        // In Java, ciphertext and tag are a single array. C# requires them separated.
        var ciphertextSize = blob.Length - HeaderSize - TagLengthBytes;
        var ciphertext = new byte[ciphertextSize];
        var tag = new byte[TagLengthBytes];

        Buffer.BlockCopy(blob, HeaderSize, ciphertext, 0, ciphertextSize);
        Buffer.BlockCopy(blob, HeaderSize + ciphertextSize, tag, 0, TagLengthBytes);

        byte[][] keysToTry = { materialKey, LegacyKey(version) };

        foreach (var key in keysToTry)
        {
            try
            {
                var plaintext = new byte[ciphertext.Length];
                using (var aesGcm = new AesGcm(key, TagLengthBytes))
                {
                    aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
                }

                return Encoding.UTF8.GetString(plaintext);
            }
            catch (CryptographicException)
            {
                // Decryption failed (wrong key or corrupted data), continue to next key
            }
        }

        return null;
    }

    private static byte[] Hmac(byte[] key, string message)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
    }

    private static byte[] LegacyKey(int version)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes($"{LegacySecret}:v{version}"));
    }

    /// <summary>
    ///     Payload structure matching the Kotlin `AaReqPayload`.
    /// </summary>
    public record AaReqPayload(int v, long ts, long epoch, string buildId, string qh, string k);
}