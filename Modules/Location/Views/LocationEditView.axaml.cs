using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Location.Views;

public partial class LocationEditView : UserControl
{
    public LocationEditView()
    {
        InitializeComponent();
    }

    private void OnHeaderContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is ContextMenu cm && cm.PlacementTarget is { DataContext: { } dc })
            cm.DataContext = dc;
    }

    private void OnBlChipTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not ViewModels.LocationEditViewModel vm) return;
        e.Handled = true;
        vm.ToBlCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
