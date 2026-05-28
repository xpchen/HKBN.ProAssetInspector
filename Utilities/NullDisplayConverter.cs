using System;
using System.Globalization;
using System.Windows.Data;

namespace HKBN.ProAssetInspector.Utilities
{
    public class NullDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "\u2014";

            if (value is string s && string.IsNullOrWhiteSpace(s))
                return "\u2014";

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}
