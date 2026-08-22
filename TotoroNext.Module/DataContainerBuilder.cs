using System.Runtime.CompilerServices;

namespace TotoroNext.Module;

public class DataContainerBuilder
{
    private IEnumerable<object> _allowedValues = [];
    private string? _description;
    private string? _displayName;
    private SpecialEditorType _editorType;
    private string _name = "";
    private object? _value = "";

    public DataContainerBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public DataContainerBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public DataContainerBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public DataContainerBuilder WithValue(object? value)
    {
        _value = value;
        return this;
    }

    public DataContainerBuilder WithNameAndValue<T>(T value, [CallerArgumentExpression(nameof(value))] string valueExpression = "")
    {
        _value = value;
        _name = valueExpression.Split('.').LastOrDefault() ?? "";
        _displayName = _name;
        return this;
    }

    public DataContainerBuilder WithAllowedValues(IEnumerable<object> allowedValues)
    {
        _allowedValues = allowedValues;
        _editorType = SpecialEditorType.ComboBox;
        return this;
    }

    public DataContainerBuilder WithEditorType(SpecialEditorType type)
    {
        _editorType = type;
        return this;
    }

    public DataContainerBuilder WithAllowedValues<T>()
        where T : struct, Enum
    {
        _allowedValues = Enum.GetValues<T>().Cast<object>();
        _editorType = SpecialEditorType.ComboBox;
        return this;
    }

    public bool HasAllowedValues()
    {
        return _allowedValues.Any();
    }

    public DataContainerProperty ToProperty()
    {
        var item = new DataContainerProperty
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