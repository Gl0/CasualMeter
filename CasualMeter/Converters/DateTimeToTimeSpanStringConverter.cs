using System;
using System.Globalization;
using System.Windows.Data;
using CasualMeter.Tracker;

namespace CasualMeter.Converters
{
    public class DateTimeToTimeSpanStringConverter :IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value != null && !(value is DateTime)) ||
                (parameter != null && !(parameter is DateTime)))
                throw new ArgumentException($"Invalid arguments passed to {nameof(DateTimeToTimeSpanStringConverter)}.");

            var currentTime = value as DateTime? ?? DateTime.Now;
            var startTime = parameter as DateTime? ?? DateTime.Now;

            var helper = FormatHelpers.Pretty;
            return helper.FormatTimeSpan(currentTime - startTime);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
