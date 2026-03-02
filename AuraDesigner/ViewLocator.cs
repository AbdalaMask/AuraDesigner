using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AuraDesigner.ViewModels;
using Dock.Model.Core;

namespace AuraDesigner;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var control = (Control)Activator.CreateInstance(type)!;
            control.DataContext = data;
            return control;
        }

        // Fallback for Dock Avalonia Models
        if (data is IDockable dockable)
        {
            return new TextBlock { Text = dockable.Title };
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase || data is IDockable;
    }
}
