using System;
using System.ComponentModel;
using System.Reflection;

namespace AuraDesigner.ViewModels;

public class PropertyItem : INotifyPropertyChanged
{
    private readonly object _targetObject;
    private readonly PropertyInfo _propertyInfo;

    public string Name => _propertyInfo.Name;

    public object? Value
    {
        get => _propertyInfo.GetValue(_targetObject);
        set
        {
            try
            {
                // Basic type conversion for numbers / strings
                object? convertedValue = value;
                if (value is string strValue && _propertyInfo.PropertyType != typeof(string))
                {
                    var converter = TypeDescriptor.GetConverter(_propertyInfo.PropertyType);
                    if (converter.CanConvertFrom(typeof(string)))
                    {
                        convertedValue = converter.ConvertFromString(strValue);
                    }
                }
                
                _propertyInfo.SetValue(_targetObject, convertedValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set property {Name}: {ex.Message}");
            }
        }
    }

    public Type PropertyType => _propertyInfo.PropertyType;

    public PropertyItem(object targetObject, PropertyInfo propertyInfo)
    {
        _targetObject = targetObject;
        _propertyInfo = propertyInfo;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
