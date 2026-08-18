using System.Runtime.CompilerServices;

namespace TotoroNext.Module;

public class ModuleOptionBuilder
{
    private IEnumerable<string> _allowedValues = [];
    private string? _description;
    private string? _displayName;
    private SpecialEditorType _editorType;
    private string _name = "";
    private string _value = "";

    public ModuleOptionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ModuleOptionBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public ModuleOptionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ModuleOptionBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public ModuleOptionBuilder WithValue<T>(T value)
    {
        _value = value?.ToString() ?? "";
        return this;
    }

    public ModuleOptionBuilder WithNameAndValue<T>(T value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
    {
        _value = value?.ToString() ?? "";
        _name = valueExpression.Split('.').LastOrDefault() ?? "";
        _displayName = _name;
        return this;
    }

    public ModuleOptionBuilder WithAllowedValues(IEnumerable<string> allowedValues)
    {
        _allowedValues = allowedValues;
        return this;
    }

    public ModuleOptionBuilder WithAllowedValues<T>(IEnumerable<T> allowedValues)
    {
        _allowedValues = allowedValues.Where(x => x is not null).Select(x => x!.ToString()!);
        return this;
    }

    public ModuleOptionBuilder WithEditorType(SpecialEditorType type)
    {
        _editorType = type;
        return this;
    }

    public ModuleOptionBuilder WithAllowedValues<T>()
        where T : struct, Enum
    {
        _allowedValues = Enum.GetNames<T>();
        return this;
    }

    public bool HasAllowedValues()
    {
        return _allowedValues.Any();
    }

    public ModuleOptionItem ToPluginOption()
    {
        var item = new ModuleOptionItem
        {
            Name = _name,
            DisplayName = string.IsNullOrEmpty(_displayName) ? _name : _displayName,
            Description = _description,
            Value = _value,
            EditorType = _editorType,
            AllowedValues = _allowedValues
        };

        Reset();
        
        return item;
    }

    private void Reset()
    {
        _name = "";
        _displayName = "";
        _displayName = "";
        _value = "";
        _editorType = default;
        _allowedValues = [];
    }
}