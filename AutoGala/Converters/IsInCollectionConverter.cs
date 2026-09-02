using Plugin.Core.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace AutoGala.Converters
{
    public class IsInCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is RebarItem item && values[1] is ObservableCollection<RebarItem> duplicates)
            {
                return duplicates.Contains(item);
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
