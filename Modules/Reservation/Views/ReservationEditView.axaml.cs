using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Reservation.Views;

public partial class ReservationEditView : UserControl
{
    public ReservationEditView()
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
        if (DataContext is not ViewModels.ReservationEditViewModel vm) return;
        e.Handled = true;
        vm.ToBlCommand.Execute(null);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
