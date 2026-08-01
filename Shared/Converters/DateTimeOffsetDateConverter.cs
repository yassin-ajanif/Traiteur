using System.Globalization;
using Avalonia.Data.Converters;

namespace GestionCommerciale.Shared.Converters;

/// <summary>
/// Bridges <see cref="DateTimeOffset"/> view-model dates to <see cref="CalendarDatePicker.SelectedDate"/> (<see cref="DateTime"/>?).
/// </summary>
public sealed class DateTimeOffsetDateConverter : IValueConverter
{
    public static readonly DateTimeOffsetDateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            DateTimeOffset dto => dto.Date,
            DateTime dt => dt.Date,
            _ => null
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
            return null;

        var date = dt.Date;
        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            return date;
        return new DateTimeOffset(date);
    }
}
