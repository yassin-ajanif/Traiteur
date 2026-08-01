using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace GestionCommerciale.Shared.Behaviors;

/// <summary>
/// Prevents mouse-wheel from changing calendar date values.
/// Forwards the scroll to the nearest parent <see cref="ScrollViewer"/> so the page still scrolls.
/// </summary>
public static class DatePickerWheelGuard
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;
        _registered = true;

        InputElement.PointerWheelChangedEvent.AddClassHandler<CalendarDatePicker>(
            OnWheel, RoutingStrategies.Tunnel);
        // Safety if any DatePicker remains
        InputElement.PointerWheelChangedEvent.AddClassHandler<DatePicker>(
            OnWheel, RoutingStrategies.Tunnel);
    }

    private static void OnWheel(Control sender, PointerWheelEventArgs e)
    {
        e.Handled = true;
        ForwardToScrollViewer(sender, e.Delta);
    }

    private static void ForwardToScrollViewer(Control control, Vector delta)
    {
        var scrollViewer = control.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer is null) return;

        const double lineHeight = 48;
        var nextY = scrollViewer.Offset.Y - (delta.Y * lineHeight);
        var maxY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        scrollViewer.Offset = new Vector(
            scrollViewer.Offset.X,
            Math.Clamp(nextY, 0, maxY));
    }
}
