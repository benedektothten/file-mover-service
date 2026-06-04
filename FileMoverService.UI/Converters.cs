using System.Globalization;
using System.Windows.Data;

namespace FileMoverService.UI;

/// <summary>Returns true when the AlternationIndex is odd — used with DataTrigger + DynamicResource for zebra rows.</summary>
public class IsOddConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int i && i % 2 == 1;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Converts a 0-based AlternationIndex to a 1-based row number string.</summary>
public class IncrementConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int i ? (i + 1).ToString() : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
