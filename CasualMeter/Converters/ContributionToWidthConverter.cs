using System;
using System.Globalization;
using System.Windows.Data;

namespace CasualMeter.Converters
{
    public class ContributionToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value != null && !(value is double)) ||
                 parameter != null && !(parameter is double))
                throw new ArgumentException($"Invalid arguments passed to {nameof(ContributionToWidthConverter)}.");

            var multiplier = double.IsNaN((double?) value ?? 0) ? 0 : ((double?)value ?? 0);
            var width = double.IsNaN((double?)parameter ?? 0) ? 0 : ((double?)parameter ?? 0);

            return multiplier * width;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
