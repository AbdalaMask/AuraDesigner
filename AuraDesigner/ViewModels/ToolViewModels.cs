using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Dock.Model.Mvvm.Controls;

using System.IO;


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


public class SolutionNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsFile { get; set; }
    public ObservableCollection<SolutionNode> Children { get; } = new();
}

public class SolutionExplorerViewModel : Tool
{
    public ObservableCollection<SolutionNode> RootNodes { get; } = new();

    public SolutionExplorerViewModel()
    {
        // Load the current solution path (hardcoded to the demo project space for now)
        string path = @"C:\Users\MyScade2026\New folder\AuraDesigner";
        
        if (Directory.Exists(path))
        {
            var root = new SolutionNode 
            { 
                Name = "Solution 'AuraDesigner'", 
                FullPath = path, 
                IsFile = false 
            };
            
            PopulateTree(path, root);
            RootNodes.Add(root);
        }
    }

    private void PopulateTree(string dir, SolutionNode node)
    {
        // Add directories
        foreach (var d in Directory.GetDirectories(dir).Where(d => !d.Contains(".git") && !d.Contains(".vs") && !d.Contains("bin") && !d.Contains("obj")))
        {
            var pNode = new SolutionNode { Name = Path.GetFileName(d), FullPath = d, IsFile = false };
            node.Children.Add(pNode);
            PopulateTree(d, pNode);
        }

        // Add files
        foreach (var f in Directory.GetFiles(dir))
        {
            node.Children.Add(new SolutionNode { Name = Path.GetFileName(f), FullPath = f, IsFile = true });
        }
    }
}

public class PropertiesViewModel : Tool
{
}
