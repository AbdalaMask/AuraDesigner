using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;
using Dock.Model.Mvvm.Controls;
using AuraDesigner.Core.Models;

namespace AuraDesigner.ViewModels;

public class PropertiesViewModel : Tool
{
    private object? _selectedObject;
    public object? SelectedObject
    {
        get => _selectedObject;
        set { _selectedObject = value; OnPropertyChanged(); }
    }

    private string _selectedObjectName = "None";
    public string SelectedObjectName
    {
        get => _selectedObjectName;
        set { _selectedObjectName = value; OnPropertyChanged(); }
    }

    public ObservableCollection<PropertyItem> Properties { get; } = new();

    public PropertiesViewModel()
    {
        SelectionService.SelectedItemChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, IDesignItem? item)
    {
        Properties.Clear();

        if (item?.Component is Control control)
        {
            SelectedObject = control;
            SelectedObjectName = control.GetType().Name;

            var controlType = control.GetType();
            
            // Only scrape standard properties useful for the designer
            var propertyInfos = controlType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && IsSupportedType(p.PropertyType))
                .OrderBy(p => p.Name)
                .ToList();

            foreach (var prop in propertyInfos)
            {
                Properties.Add(new PropertyItem(control, prop));
            }
        }
        else
        {
            SelectedObject = null;
            SelectedObjectName = "None";
        }
    }

    private bool IsSupportedType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(int) ||
               type == typeof(double) ||
               type == typeof(Brush) ||
               type == typeof(IBrush) ||
               type == typeof(Thickness) ||
               type.IsEnum;
    }

}
