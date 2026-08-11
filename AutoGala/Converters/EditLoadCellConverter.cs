using Plugin.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace AutoGala.Converters
{
    public class EditLoadCellConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var rowItem = values[0];
            var selectedItem = values[1];
            var isGridReadOnly = values[2] is bool b && b;

            return isGridReadOnly || !ReferenceEquals(rowItem, selectedItem);

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
