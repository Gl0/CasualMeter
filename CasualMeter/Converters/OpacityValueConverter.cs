using System;
using System.Globalization;
using System.Windows.Data;

namespace CasualMeter.Converters
{
    public class OpacityValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (value != null && !(value is double)))
                throw new ArgumentException($"Invalid arguments passed to {nameof(OpacityValueConverter)}.");

            var modifiedValue = (double)value * 1;
            return modifiedValue > 1 ? 1 : modifiedValue < 0.5 ? 0.5 : modifiedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
