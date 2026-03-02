using System.Collections.ObjectModel;


namespace AuraDesigner.ViewModels;

public class SolutionNode
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsFile { get; set; }
    public ObservableCollection<SolutionNode> Children { get; } = new();
}
