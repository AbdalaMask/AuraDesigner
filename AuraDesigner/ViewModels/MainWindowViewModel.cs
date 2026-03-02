using Dock.Model.Controls;
using Dock.Model.Core;

namespace AuraDesigner.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private IFactory? _factory;
    private IRootDock? _layout;

    public IFactory? Factory
    {
        get => _factory;
        set
        {
            if (_factory != value)
            {
                _factory = value;
                OnPropertyChanged();
            }
        }
    }

    public IRootDock? Layout
    {
        get => _layout;
        set
        {
            if (_layout != value)
            {
                _layout = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindowViewModel()
    {
        Factory = new AuraDockFactory();
        Layout = Factory?.CreateLayout();
        if (Layout != null)
        {
            Factory?.InitLayout(Layout);
        }
    }
}
