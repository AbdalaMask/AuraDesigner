using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Input;
using Avalonia.Styling;
using System.Reflection;

namespace AuraDesigner.Core;

public static class PropertyConverter
{
    public static object? ConvertValue(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(Thickness)) return Thickness.Parse(value);
            if (targetType == typeof(CornerRadius)) return CornerRadius.Parse(value);
            if (targetType == typeof(Color)) return Color.Parse(value);
            if (targetType == typeof(Vector)) return Vector.Parse(value);
            if (targetType == typeof(Point)) return Point.Parse(value);
            if (targetType == typeof(Size)) return Size.Parse(value);
            if (targetType == typeof(Cursor)) return Cursor.Parse(value);
            if (targetType == typeof(RelativePoint)) return RelativePoint.Parse(value);
            if (targetType == typeof(RelativeScalar)) return RelativeScalar.Parse(value);
            if (targetType == typeof(FontFamily)) return new FontFamily(value);
            if (targetType == typeof(FontWeight)) return FontWeight.Parse(typeof(FontWeight), value);
            if (targetType == typeof(FontStyle)) return Enum.Parse(typeof(FontStyle), value);
            if (targetType == typeof(Geometry)) return Geometry.Parse(value);

            // Selector usually has a TypeConverter registered by Avalonia.
            // If explicit Parse isn't available, we'll let it fall through to TypeDescriptor.

            if (targetType == typeof(AvaloniaProperty))
            {
                return ResolveAvaloniaProperty(value);
            }

            if (targetType.IsEnum) return Enum.Parse(targetType, value);

            if (typeof(IBrush).IsAssignableFrom(targetType))
            {
                return Brush.Parse(value);
            }

            if (typeof(IImage).IsAssignableFrom(targetType))
            {
                // This is a simplified approach, might need better path resolution
                try { return new Bitmap(value); } catch { return null; }
            }

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                return converter.ConvertFromInvariantString(value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Conversion error for {targetType.Name}: {ex.Message}");
        }

        return value; // Fallback to raw string, which might fail later during SetValue
    }

    private static AvaloniaProperty? ResolveAvaloniaProperty(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // Try to handle owner.property syntax
        if (name.Contains("."))
        {
            var parts = name.Split('.');
            var ownerTypeName = parts[0];
            var propertyName = parts[1];

            // We'd need XamlParser's ResolveType here, but we can't easily access it without circular dependency
            // For now, let's just try to find it in loaded assemblies for common Avalonia types
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("Avalonia.Controls." + ownerTypeName)
                        ?? assembly.GetType("Avalonia." + ownerTypeName)
                        ?? assembly.GetType("Avalonia.Styling." + ownerTypeName)
                        ?? assembly.GetType("Avalonia.Animation." + ownerTypeName)
                        ?? assembly.GetType("Avalonia.Media." + ownerTypeName);
                
                if (type != null)
                {
                    var field = type.GetField(propertyName + "Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (field?.GetValue(null) is AvaloniaProperty ap) return ap;
                }
            }
        }

        // If no owner, it's hard to guess. We'll try common owners.
        string[] commonOwners = { "Control", "TemplatedControl", "Visual", "Layoutable", "StyledElement", "TextBlock", "Shape" };
        foreach (var owner in commonOwners)
        {
            var ap = ResolveAvaloniaProperty($"{owner}.{name}");
            if (ap != null) return ap;
        }
        
        return null;
    }
}
