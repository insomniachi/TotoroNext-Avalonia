using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Ursa.Controls;
using NumericUpDown = Ursa.Controls.NumericUpDown;

namespace TotoroNext.Module.Controls;

public class DataContainerPropertyTemplateSelector : IDataTemplate
{
    public Control? Build(object? param)
    {
        return param is DataContainerProperty item ? CreateSettingsCard(item) : null;
    }

    public bool Match(object? data)
    {
        return data is DataContainerProperty;
    }

    private static SettingsCard CreateSettingsCard(DataContainerProperty property)
    {
        var card = new SettingsCard
        {
            Header = property.DisplayName ?? "",
            Description = property.Description ?? "",
            Editor = CreateEditor(property),
        };

        if (!string.IsNullOrEmpty(property.IconKey))
        {
            card.Icon = IconRegistry.GetGeometry(property.IconKey);
        }

        return card;
    }

    private static Control CreateEditor(DataContainerProperty property)
    {
        return property.EditorType switch
        {
            SpecialEditorType.TextBox => CreateTextBox(property),
            SpecialEditorType.ComboBox => CreateComboBox(property),
            SpecialEditorType.ToggleSwitch => CreateToggleSwitch(property),
            SpecialEditorType.FileBrowser => CreatePathPicker(property),
            SpecialEditorType.NumberBox => CreateNumberBox(property),
            _ => throw new UnreachableException()
        };
    }

    private static NumericUpDown CreateNumberBox(DataContainerProperty property)
    {
        switch (property.Value)
        {
            case int:
            {
                var control = new NumericIntUpDown();
                control.Bind(NumericIntUpDown.ValueProperty, new Binding(nameof(property.Value))
                {
                    Source = property,
                    Mode = BindingMode.TwoWay
                });
                return control;
            }
            case double:
            {
                var control = new NumericDoubleUpDown();
                control.Bind(NumericDoubleUpDown.ValueProperty, new Binding(nameof(property.Value))
                {
                    Source = property,
                    Mode = BindingMode.TwoWay
                });
                return control;
            }
            case float:
            {
                var control = new NumericFloatUpDown();
                control.Bind(NumericFloatUpDown.ValueProperty, new Binding(nameof(property.Value))
                {
                    Source = property,
                    Mode = BindingMode.TwoWay
                });
                return control;
            }
            default:
                var defaultControl = new NumericIntUpDown();
                defaultControl.Bind(NumericIntUpDown.ValueProperty, new Binding(nameof(property.Value))
                {
                    Source = property,
                    Mode = BindingMode.TwoWay
                });
                return defaultControl;
        }
    }

    private static PathPicker CreatePathPicker(DataContainerProperty property)
    {
        return new PathPicker()
               .Title("Browse")
               .AllowMultiple(false)
               .UsePickerType(UsePickerTypes.OpenFile)
               .HorizontalAlignment(HorizontalAlignment.Stretch)
               .MinWidth(300)
               .SelectedPathsText(property, x => x.Value);
    }

    private static ToggleSwitch CreateToggleSwitch(DataContainerProperty property)
    {
        return new ToggleSwitch().IsChecked(property, x => x.Value);
    }

    private static Control CreateComboBox(DataContainerProperty property)
    {
        if (property.Value is Enum enumValue)
        {
            return new EnumSelector()
                   .DisplayDescription(true)
                   .HorizontalAlignment(HorizontalAlignment.Stretch)
                   .Value(property, x => x.Value)
                   .EnumType(enumValue.GetType());
        }
        
        return new ComboBox()
               .HorizontalAlignment(HorizontalAlignment.Stretch)
               .SelectedItem(property, x => x.Value)
               .ItemsSource(property, x => x.AllowedValues);
    }

    private static TextBox CreateTextBox(DataContainerProperty property)
    {
        return new TextBox()
               .MaxWidth(350)
               .MinWidth(200)
               .TextWrapping(TextWrapping.WrapWithOverflow)
               .Text(property, x => x.Value);
    }
}