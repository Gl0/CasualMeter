using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CasualMeter.Converters
{
    public class BooleanToContributionBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && !(value is bool))
                throw new ArgumentException($"Invalid arguments passed to {nameof(BooleanToContributionBrushConverter)}.");

            var boolValue = value as bool? ?? false;
            return boolValue
                ? (SolidColorBrush)new BrushConverter().ConvertFrom("#666699")
                : (SolidColorBrush)new BrushConverter().ConvertFrom("#12676c");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
