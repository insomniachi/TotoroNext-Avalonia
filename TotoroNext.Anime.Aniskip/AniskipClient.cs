using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotoroNext.Anime.Aniskip;

public interface IAniskipClient
{
    Task<GetSkipTimesResponseV2> GetSkipTimes(long animeId, double episodeNumber, GetSkipTimesQueryV2 query);
}

public class AniskipClient(IHttpClientFactory httpClientFactory) : IAniskipClient
{
    public async Task<GetSkipTimesResponseV2> GetSkipTimes(long animeId, double episodeNumber,
                                                           GetSkipTimesQueryV2 query)
    {
        using var client = httpClientFactory.CreateClient(nameof(AniskipClient));
        var types = string.Join("&", query.Types.Select(x => $"types={x.ToEnumString()}"));

        return await
                   client
                       .GetFromJsonAsync<
                           GetSkipTimesResponseV2>($"v2/skip-times/{animeId}/{episodeNumber}?episodeLength={query.EpisodeLength}&{types}") ??
               new GetSkipTimesResponseV2();
    }
}

public class PostVoteRequestBodyV2
{
    [JsonPropertyName("voteType")]
    [JsonConverter(typeof(JsonStringEnumConverterEx<VoteType>))]
    public VoteType VoteType { get; set; }
}

public class PostVoteResponseV2
{
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

public enum VoteType
{
    [EnumMember(Value = "upvote")] Upvote,

    [EnumMember(Value = "downvote")] Downvote
}

public class PostCreateSkipTimeRequestBodyV2
{
    [JsonPropertyName("skipType")]
    [JsonConverter(typeof(JsonStringEnumConverterEx<SkipType>))]
    public SkipType SkipType { get; set; }

    [JsonPropertyName("providerName")] public string ProviderName { get; set; } = "";

    [JsonPropertyName("startTime")] public double StartTime { get; set; }

    [JsonPropertyName("endTime")] public double EndTime { get; set; }

    [JsonPropertyName("episodeLength")] public double EpisodeLength { get; set; }

    [JsonPropertyName("submitterId")]
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid SubmitterId { get; set; }
}

public class PostCreateSkipTimeResponseV2
{
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; } = "";

    [JsonPropertyName("skipId")]
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid SkipId { get; set; }
}

public class GetSkipTimesResponseV2
{
    [JsonPropertyName("status")] public int StatusCode { get; set; }

    [JsonPropertyName("message")] public string Message { get; set; } = "";

    [JsonPropertyName("found")] public bool IsFound { get; set; }

    [JsonPropertyName("results")] public SkipTime[] Results { get; set; } = [];
}

public class GetSkipTimesQueryV2
{
    public SkipType[] Types { get; set; } = [];

    public double EpisodeLength { get; set; }
}

public class SkipTime
{
    [JsonPropertyName("interval")] public Interval Interval { get; set; } = new();

    [JsonPropertyName("skipType")]
    [JsonConverter(typeof(JsonStringEnumConverterEx<SkipType>))]
    public SkipType SkipType { get; set; }

    [JsonPropertyName("skipId")]
    [JsonConverter(typeof(GuidJsonConverter))]
    public Guid SkipId { get; set; }

    [JsonPropertyName("episodeLength")] public double EpisodeLength { get; set; }
}

public class Interval
{
    [JsonPropertyName("startTime")] public double StartTime { get; set; }

    [JsonPropertyName("endTime")] public double EndTime { get; set; }
}

public enum SkipType
{
    [EnumMember(Value = "op")] Opening,

    [EnumMember(Value = "ed")] Ending,

    [EnumMember(Value = "mixed-op")] MixedOpening,

    [EnumMember(Value = "mixed-ed")] MixedEnding,

    [EnumMember(Value = "recap")] Recap
}

public class GuidJsonConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        _ = Guid.TryParse(reader.GetString(), out var guid);
        return guid;
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("D"));
    }
}

public class JsonStringEnumConverterEx<T> : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.FromEnumString<T>();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToEnumString());
    }
}

public static class EnumExtensions
{
	extension<TField>(TField field) where TField : Enum
	{
		public string ToEnumString()
		{
			var fieldInfo = typeof(TField).GetField(field.ToString()) ??
							throw new UnreachableException($"Field {nameof(field)} was not found.");

			var attributes = (EnumMemberAttribute[])fieldInfo.GetCustomAttributes(typeof(EnumMemberAttribute), false);
			if (attributes.Length == 0)
			{
				throw
					new NotImplementedException($"The field has not been annotated with a {nameof(EnumMemberAttribute)}.");
			}

			var value = attributes[0].Value ??
						throw new
							NotImplementedException($"{nameof(EnumMemberAttribute)}.{nameof(EnumMemberAttribute.Value)} has not been set for this field.");

			return value;
		}
	}

	extension(string str)
	{
		public TField FromEnumString<TField>() where TField : Enum
		{
			var fields = typeof(TField).GetFields();
			foreach (var field in fields)
			{
				var attributes = (EnumMemberAttribute[])field.GetCustomAttributes(typeof(EnumMemberAttribute), false);
				if (attributes.Length == 0)
				{
					continue;
				}

				var value = attributes[0].Value ??
							throw new
								NotImplementedException($"{nameof(EnumMemberAttribute)}.{nameof(EnumMemberAttribute.Value)} has not been set for the field {field.Name}.");

				if (string.Equals(value, str, StringComparison.OrdinalIgnoreCase))
				{
					return (TField)Enum.Parse(typeof(TField), field.Name) ?? throw new ArgumentNullException(field.Name);
				}
			}

			throw new InvalidOperationException($"'{str}' was not found in enum {typeof(TField).Name}.");
		}
	}
}