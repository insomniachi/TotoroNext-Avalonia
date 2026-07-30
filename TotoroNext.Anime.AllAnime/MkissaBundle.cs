using System.Text;
using System.Text.RegularExpressions;

namespace TotoroNext.Anime.AllAnime;

/// <summary>
///     Recovers `buildId` and the four mask seeds from the obfuscated JS chunk. The seeds are lookups
///     into a string table rotated at load time by an amount only the bundle's checksum loop knows, so
///     <see cref="Parse" /> tries every rotation and keeps the one whose results all have the seed shape.
/// </summary>
public static class MKissaBundle
{
    // Added here for completeness since the original referenced an external MKissaCrypto.SEED_COUNT
    // The regex `{3}` implies 4 total seeds.
    private const int SeedCount = 4;

    private const string CallPattern = @"(\w+)\(\s*(-?\d+)\s*(?:,\s*(-?\d+)\s*)?\)";

    private static readonly Regex BuildIdRegex = new("""!==\s*["']string["']\s*\?\s*["'](\d+)["']\s*:\s*["']["']""", RegexOptions.Compiled);

    private static readonly Regex TableHeadRegex = new(@"function (\w+)\(\)\s*\{\s*(?:const|let|var)\s+\w+\s*=\s*\[", RegexOptions.Compiled);

    private static readonly Regex BaseDecoderRegex =
        new(@"function (\w+)\((\w+)(?:,\w+)*\)\{return \2=\2-\(?([-\d+*\s]+?)\)?,(\w+)\(\)\[\2\]\}", RegexOptions.Compiled);

    // Two parameters exactly: the argIndex logic only distinguishes first from second.
    private static readonly Regex AliasDecoderRegex =
        new(@"function (\w+)\((\w+),(\w+)\)\{return (\w+)\((\w+)((?:[-+][\d+*\s-]+)?)\)\}", RegexOptions.Compiled);

    private static readonly Regex CallRegex = new(CallPattern, RegexOptions.Compiled);

    // Uses {{3}} to escape braces for C# string interpolation
    private static readonly Regex SeedArrayRegex =
        new($@"=\[((?:{CallPattern}\+{CallPattern},){{3}}{CallPattern}\+{CallPattern})]", RegexOptions.Compiled);

    // Anchored with ^ and $ because Kotlin's Regex.matches checks the entire string.
    private static readonly Regex SeedRegex = new(@"^[A-Za-z0-9+/]{11}=$", RegexOptions.Compiled);

    // Leading signs matched greedily; `Fold` counts them.
    private static readonly Regex TermRegex = new(@"[-+]*[^-+]+", RegexOptions.Compiled);

    public static BuildInfo? Parse(string js)
    {
        var buildIdMatch = BuildIdRegex.Match(js);
        if (!buildIdMatch.Success)
        {
            return null;
        }

        var buildId = buildIdMatch.Groups[1].Value;
        var seeds = ExtractSeeds(js);
        return seeds == null ? null : new BuildInfo(buildId, seeds);
    }

    private static List<string>? ExtractSeeds(string js)
    {
        var tables = ReadTables(js);

        var bases = new Dictionary<string, Base>();
        foreach (Match m in BaseDecoderRegex.Matches(js))
        {
            bases[m.Groups[1].Value] = new Base(m.Groups[4].Value, Fold(m.Groups[3].Value));
        }

        var aliases = new Dictionary<string, Alias>();
        // A seed may call a base decoder directly, so each base is its own identity alias.
        foreach (var key in bases.Keys)
        {
            aliases[key] = new Alias(key, 0, 0);
        }

        foreach (Match m in AliasDecoderRegex.Matches(js))
        {
            var name = m.Groups[1].Value;
            var firstParam = m.Groups[2].Value;
            var callee = m.Groups[4].Value;
            var arg = m.Groups[5].Value;
            var delta = m.Groups[6].Value;

            if (!bases.ContainsKey(callee))
            {
                continue;
            }

            // Which parameter the alias forwards tells us where the table index sits.
            aliases[name] = new Alias(
                                      callee,
                                      arg == firstParam ? 0 : 1,
                                      string.IsNullOrEmpty(delta) ? 0 : Fold(delta)
                                     );
        }

        foreach (Match match in SeedArrayRegex.Matches(js))
        {
            var calls = CallRegex.Matches(match.Groups[1].Value)
                                 .Select(m => m.Value)
                                 .ToList();

            if (calls.Count != SeedCount * 2)
            {
                continue;
            }

            var firstCallMatch = CallRegex.Match(calls.First());
            if (!firstCallMatch.Success)
            {
                continue;
            }

            var firstAliasName = firstCallMatch.Groups[1].Value;
            if (!aliases.TryGetValue(firstAliasName, out var firstAlias))
            {
                continue;
            }

            if (!bases.TryGetValue(firstAlias.BaseStr, out var firstBase))
            {
                continue;
            }

            if (!tables.TryGetValue(firstBase.Table, out var table))
            {
                continue;
            }

            var matches = Enumerable.Range(0, table.Count)
                                    .Select(rotation => SeedsAt(calls, rotation, tables, bases, aliases))
                                    .Where(s => s != null)
                                    .ToList();

            // A chance match would silently yield a bad mask, so require an unambiguous answer.
            if (matches.Count == 1)
            {
                return matches[0];
            }
        }

        return null;
    }

    private static List<string>? SeedsAt(
        List<string> calls,
        int rotation,
        Dictionary<string, List<string>> tables,
        Dictionary<string, Base> bases,
        Dictionary<string, Alias> aliases)
    {
        var seeds = new List<string>();
        for (var i = 0; i < calls.Count; i += 2)
        {
            var first = calls[i];
            var second = calls[i + 1];

            var a = Resolve(first, rotation, tables, bases, aliases);
            if (a == null)
            {
                return null;
            }

            var b = Resolve(second, rotation, tables, bases, aliases);
            if (b == null)
            {
                return null;
            }

            var combined = a + b;
            if (!SeedRegex.IsMatch(combined))
            {
                return null;
            }

            seeds.Add(combined);
        }

        return seeds.Count == SeedCount ? seeds : null;
    }

    private static string? Resolve(
        string call,
        int rotation,
        Dictionary<string, List<string>> tables,
        Dictionary<string, Base> bases,
        Dictionary<string, Alias> aliases)
    {
        var match = CallRegex.Match(call);
        if (!match.Success)
        {
            return null;
        }

        if (!aliases.TryGetValue(match.Groups[1].Value, out var alias))
        {
            return null;
        }

        if (!bases.TryGetValue(alias.BaseStr, out var baseObj))
        {
            return null;
        }

        if (!tables.TryGetValue(baseObj.Table, out var table) || table.Count == 0)
        {
            return null;
        }

        var args = new List<int>();
        if (int.TryParse(match.Groups[2].Value, out var arg1))
        {
            args.Add(arg1);
        }

        if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out var arg2))
        {
            args.Add(arg2);
        }

        if (alias.ArgIndex >= args.Count)
        {
            return null;
        }

        var arg = args[alias.ArgIndex];

        var index = arg + alias.Delta - baseObj.Offset + rotation;

        // C# % can return negative, so adjust properly
        var positiveIndex = (index % table.Count + table.Count) % table.Count;
        return table[positiveIndex];
    }

    private static Dictionary<string, List<string>> ReadTables(string js)
    {
        var tables = new Dictionary<string, List<string>>();
        foreach (Match match in TableHeadRegex.Matches(js))
        {
            // match.Index + match.Length - 1 gives the index of '[' which corresponds to match.range.last in Kotlin
            var array = ReadStringArray(js, match.Index + match.Length - 1);
            if (array != null)
            {
                tables[match.Groups[1].Value] = array;
            }
        }

        return tables;
    }

    /// <summary>
    ///     Whitelist parser: returns null rather than a partial array if anything unexpected appears.
    /// </summary>
    private static List<string>? ReadStringArray(string js, int open)
    {
        var items = new List<string>();
        var i = open + 1;
        while (i < js.Length)
        {
            var c = js[i];
            switch (c)
            {
                case ']':
                    return items;
                case ',':
                case ' ':
                    i++;
                    break;
                case '"':
                case '\'':
                {
                    var sb = new StringBuilder();
                    i++;
                    while (i < js.Length && js[i] != c)
                    {
                        if (js[i] == '\\')
                        {
                            if (i + 1 >= js.Length)
                            {
                                return null;
                            }

                            sb.Append(js[i + 1]);
                            i += 2;
                        }
                        else
                        {
                            sb.Append(js[i]);
                            i++;
                        }
                    }

                    if (i >= js.Length)
                    {
                        return null;
                    }

                    i++;
                    items.Add(sb.ToString());
                    break;
                }
                default:
                    return null;
            }
        }

        return null;
    }

    /// <summary>
    ///     Folds the `2935+-1459*2` arithmetic every integer is hidden behind; signs stack.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static int Fold(string expression)
    {
        var total = 0;
        var noSpaces = expression.Replace(" ", "");

        foreach (Match m in TermRegex.Matches(noSpaces))
        {
            var term = m.Value;
            var sign = 1;
            var body = term;

            while (body.StartsWith('+') || body.StartsWith('-'))
            {
                if (body.StartsWith('-'))
                {
                    sign = -sign;
                }

                body = body[1..];
            }

            var value = 1;
            foreach (var factor in body.Split('*'))
            {
                if (int.TryParse(factor, out var parsedVal))
                {
                    value *= parsedVal;
                }
                else
                {
                    return 0;
                }
            }

            total += sign * value;
        }

        return total;
    }

    public class BuildInfo(string buildId, List<string> seeds)
    {
        public string BuildId { get; } = buildId;
        public List<string> Seeds { get; } = seeds;
    }

    private class Base(string table, int offset)
    {
        public string Table { get; } = table;
        public int Offset { get; } = offset;
    }

    private class Alias(string baseStrStr, int argIndex, int delta)
    {
        public string BaseStr { get; } = baseStrStr;
        public int ArgIndex { get; } = argIndex;
        public int Delta { get; } = delta;
    }
}