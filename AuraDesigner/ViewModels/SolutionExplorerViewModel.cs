using System;
using System.Collections.ObjectModel;
using System.Linq;
using Dock.Model.Mvvm.Controls;

using System.IO;


namespace AuraDesigner.ViewModels;

public class SolutionExplorerViewModel : Tool
{
    public ObservableCollection<SolutionNode> RootNodes { get; } = new();
    private string? _projectRoot;
    public string? ProjectRoot 
    { 
        get => _projectRoot; 
        private set => SetProperty(ref _projectRoot, value); 
    }

    public event EventHandler<string>? FileOpenRequested;

    public SolutionExplorerViewModel()
    {
    }

    public void LoadProject(string path)
    {
        RootNodes.Clear();
        
        // If the user selected a .sln or .csproj, we want to load the folder it resides in.
        bool isProjectFile = false;
        string targetDirectory = path;

        if (File.Exists(path))
        {
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || 
                path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                isProjectFile = true;
                targetDirectory = Path.GetDirectoryName(path) ?? path;
            }
        }

        if (Directory.Exists(targetDirectory))
        {
            ProjectRoot = targetDirectory;
            var root = new SolutionNode 
            { 
                Name = isProjectFile ? $"Project '{Path.GetFileName(path)}'" : $"Folder '{Path.GetFileName(targetDirectory)}'", 
                FullPath = targetDirectory, 
                IsFile = false 
            };
            
            PopulateTree(targetDirectory, root);
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
