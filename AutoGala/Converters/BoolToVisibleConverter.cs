using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoGala.Converters
{
    public class BoolToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine($"Convert called: value={value}, parameter={parameter}");
            if (value is bool enabled)
            {
                if (parameter?.ToString() == "Invert")
                    enabled = !enabled;

                return enabled ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new Exception("Value is not of type bool");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visible)
            {
                var result = visible == Visibility.Visible;

                if (parameter?.ToString() == "Invert")
                    result = !result;

                return result;
            }

            throw new Exception("Value is not of type Visibility");
        }
    }
}
