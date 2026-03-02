using Avalonia.Controls;
using AuraDesigner.ViewModels;
using System.Threading.Tasks;

namespace AuraDesigner.Views;

public partial class NewProjectWindow : Window
{
    public NewProjectWindow()
    {
        InitializeComponent();
        var vm = new NewProjectViewModel();
        vm.RequestClose += (result) => Close(result);
        DataContext = vm;
    }
}
