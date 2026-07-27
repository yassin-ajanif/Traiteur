using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Location.Views;

public partial class EtatClientView : UserControl
{
    public EtatClientView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is ViewModels.EtatClientViewModel vm)
            vm.LoadCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
