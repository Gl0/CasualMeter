using System;
using System.Globalization;
using System.Windows.Data;
using CasualMeter.Tracker;

namespace CasualMeter.Converters
{
    public class DpsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && !(value is long))
                throw new ArgumentException($"Invalid arguments passed to {nameof(DpsToStringConverter)}.");

            var helper = FormatHelpers.Pretty;
            return $"{helper.FormatValue((long?) value ?? 0)}/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
