using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Dock.Model.Mvvm.Controls;


namespace AuraDesigner.ViewModels;
public class ToolboxViewModel : Tool
{
    public ObservableCollection<Type> AvailableControls { get; } = new();

    public ToolboxViewModel()
    {
        // Load common Avalonia controls using reflection on the Control assembly
        var controlAssembly = typeof(Control).Assembly;
        
        var controlTypes = controlAssembly.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && t.IsSubclassOf(typeof(Control)))
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in controlTypes)
        {
            AvailableControls.Add(type);
        }
    }
}
