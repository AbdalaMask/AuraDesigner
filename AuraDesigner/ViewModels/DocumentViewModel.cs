using Dock.Model.Mvvm.Controls;

namespace AuraDesigner.ViewModels;

public class DocumentViewModel : Document
{
    private AuraDesigner.Core.Models.IDesignItem? _rootItem;
    public AuraDesigner.Core.Models.IDesignItem? RootItem
    {
        get => _rootItem;
        set => SetProperty(ref _rootItem, value);
    }
}

public class CodeDocumentViewModel : Document
{
    // For .cs, .js, .md etc files
}
