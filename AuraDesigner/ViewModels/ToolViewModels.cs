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

    public event EventHandler<string>? FileOpenRequested;

    public SolutionExplorerViewModel()
    {
    }

    public void LoadProject(string path)
    {
        RootNodes.Clear();
        if (Directory.Exists(path))
        {
            var root = new SolutionNode 
            { 
                Name = $"Project '{Path.GetFileName(path)}'", 
                FullPath = path, 
                IsFile = false 
            };
            
            PopulateTree(path, root);
            RootNodes.Add(root);
        }
        else if (File.Exists(path))
        {
            // If they just opened a single file instead of a folder
            var root = new SolutionNode 
            { 
                Name = $"File '{Path.GetFileName(path)}'", 
                FullPath = path, 
                IsFile = true 
            };
            RootNodes.Add(root);
            
            // Auto open the file
            FileOpenRequested?.Invoke(this, path);
        }
    }

    public void RequestOpenFile(SolutionNode node)
    {
        if (node != null && node.IsFile)
        {
            FileOpenRequested?.Invoke(this, node.FullPath);
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
