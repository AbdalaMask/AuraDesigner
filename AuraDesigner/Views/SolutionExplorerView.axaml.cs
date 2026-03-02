using Avalonia.Controls;

namespace AuraDesigner.Views;

public partial class SolutionExplorerView : UserControl
{
    public SolutionExplorerView()
    {
        InitializeComponent();
    }

    private void OnItemDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is TreeView tree && tree.SelectedItem is ViewModels.SolutionNode node)
        {
            if (DataContext is ViewModels.SolutionExplorerViewModel vm)
            {
                vm.RequestOpenFile(node);
            }
        }
    }
}
