using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class LocalSettingsService : ILocalSettingsService
{
    private readonly JsonObject _settings = [];
    private readonly string _file;
    private readonly JsonSerializerOptions _options;

    public LocalSettingsService()
    {
        _options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter()
            },
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        _file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", "settings.json");
        if (File.Exists(_file))
        {
            _settings = JsonNode.Parse(File.ReadAllText(_file))!.AsObject();
        }
    }

    public T? ReadSetting<T>(string key, T? deafultValue = default)
    {
        if (!_settings.ContainsKey(key))
        {
            SaveSetting(key, deafultValue);
            return deafultValue;
        }

        return _settings[key].Deserialize<T>(_options);
    }

    public void RemoveSetting(string key)
    {
        _settings.Remove(key);
    }

    public void SaveSetting<T>(string key, T value)
    {
        _settings[key] = JsonNode.Parse(JsonSerializer.Serialize(value, _options));
        File.WriteAllText(_file, _settings.ToJsonString(_options));
    }
}
