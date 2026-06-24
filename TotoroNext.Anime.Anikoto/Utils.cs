using System.Text;

namespace TotoroNext.Anime.Anikoto;

public static class Utils
{
    private const string KEY_1 = "ItFKjuWokn4ZpB";
    private const string KEY_2 = "fOyt97QWFB3";
    private const string KEY_3 = "736y1uTJpBLUX";
    private static readonly string[] EXCHANGE_KEY_1 = { "AP6GeR8H0lwUz1", "UAz8Gwl10P6ReH" };
    private static readonly string[] EXCHANGE_KEY_2 = { "1majSlPQd2M5", "da1l2jSmP5QM" };
    private static readonly string[] EXCHANGE_KEY_3 = { "CPYvHj09Au3", "0jHA9CPYu3v" };

    private static readonly List<(int Step, string Action, string[] Args)> ORDER = new()
    {
        (1, "exchange", EXCHANGE_KEY_1),
        (2, "rc4", new[] { KEY_1 }),
        (3, "rc4", new[] { KEY_2 }),
        (4, "exchange", EXCHANGE_KEY_2),
        (5, "exchange", EXCHANGE_KEY_3),
        (6, "reverse", Array.Empty<string>()),
        (7, "rc4", new[] { KEY_3 }),
        (8, "base64", Array.Empty<string>())
    };

    // --- ENCRYPTION ---

    public static string VrfEncrypt(string input)
    {
        var vrf = input;

        foreach (var item in ORDER)
        {
            switch (item.Action)
            {
                case "exchange":
                    vrf = Exchange(vrf, item.Args);
                    break;
                case "rc4":
                    vrf = Rc4Encrypt(item.Args[0], vrf);
                    break;
                case "reverse":
                    var charArray = vrf.ToCharArray();
                    Array.Reverse(charArray);
                    vrf = new string(charArray);
                    break;
                case "base64":
                    var bytes = Encoding.UTF8.GetBytes(vrf);
                    vrf = Convert.ToBase64String(bytes)
                                 .Replace('+', '-')
                                 .Replace('/', '_');
                    break;
            }
        }

        return Uri.EscapeDataString(vrf);
    }

    private static string Rc4Encrypt(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);

        var outputBytes = ApplyRC4(keyBytes, inputBytes);

        return Convert.ToBase64String(outputBytes)
                      .Replace('+', '-')
                      .Replace('/', '_');
    }

    // --- DECRYPTION ---

    public static string VrfDecrypt(string input)
    {
        // 1. Undo the URL encoding first
        var vrf = Uri.UnescapeDataString(input);

        // 2. Iterate through the steps in REVERSE order
        for (var i = ORDER.Count - 1; i >= 0; i--)
        {
            var item = ORDER[i];
            switch (item.Action)
            {
                case "exchange":
                    // Swap the keys: look in Key 2, replace with Key 1
                    vrf = Exchange(vrf, new[] { item.Args[1], item.Args[0] });
                    break;
                case "rc4":
                    vrf = Rc4Decrypt(item.Args[0], vrf);
                    break;
                case "reverse":
                    var charArray = vrf.ToCharArray();
                    Array.Reverse(charArray);
                    vrf = new string(charArray);
                    break;
                case "base64":
                    var bytes = DecodeUrlSafeBase64(vrf);
                    vrf = Encoding.UTF8.GetString(bytes);
                    break;
            }
        }

        return vrf;
    }

    private static string Rc4Decrypt(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);

        // Input is URL-safe Base64, so we must decode it to bytes first
        var inputBytes = DecodeUrlSafeBase64(input);

        // RC4 is symmetric: running the cipher on the encrypted bytes decrypts them
        var outputBytes = ApplyRC4(keyBytes, inputBytes);

        return Encoding.UTF8.GetString(outputBytes);
    }

    // --- SHARED UTILITIES ---

    private static string Exchange(string input, string[] keys)
    {
        var key1 = keys[0];
        var key2 = keys[1];

        var result = new char[input.Length];

        for (var i = 0; i < input.Length; i++)
        {
            var idx = key1.IndexOf(input[i]);
            result[i] = idx != -1 ? key2[idx] : input[i];
        }

        return new string(result);
    }

    /// <summary>
    ///     Decodes URL-safe Base64 strings and automatically appends missing padding.
    /// </summary>
    private static byte[] DecodeUrlSafeBase64(string input)
    {
        // Revert URL-safe characters back to standard Base64 characters
        var base64 = input.Replace('-', '+').Replace('_', '/');

        // C# requires Base64 strings to be properly padded with '='
        var padLength = 4 - base64.Length % 4;
        if (padLength < 4)
        {
            base64 = base64.PadRight(base64.Length + padLength, '=');
        }

        return Convert.FromBase64String(base64);
    }

    private static byte[] ApplyRC4(byte[] key, byte[] input)
    {
        var s = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        for (int i = 0, j = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) % 256;
            var temp = s[i];
            s[i] = s[j];
            s[j] = temp;
        }

        var output = new byte[input.Length];
        for (int a = 0, i = 0, j = 0; a < input.Length; a++)
        {
            i = (i + 1) % 256;
            j = (j + s[i]) % 256;

            var temp = s[i];
            s[i] = s[j];
            s[j] = temp;

            output[a] = (byte)(input[a] ^ s[(s[i] + s[j]) % 256]);
        }

        return output;
    }
}