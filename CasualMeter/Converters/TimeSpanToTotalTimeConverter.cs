using System;
using System.Globalization;
using System.Windows.Data;
using CasualMeter.Tracker;

namespace CasualMeter.Converters
{
    public class TimeSpanToTotalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
                throw new ArgumentException($"Invalid arguments passed to {nameof(TimeSpanToTotalTimeConverter)}.");
            
            var helper = FormatHelpers.Pretty;
            return $"Total time: {helper.FormatTimeSpan((TimeSpan)value)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
