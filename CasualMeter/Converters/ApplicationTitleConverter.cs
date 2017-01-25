using System;
using System.Globalization;
using System.Windows.Data;
using Tera.Game;

namespace CasualMeter.Converters
{
    public class ApplicationTitleConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && !(value is Server)) 
                throw new ArgumentException($"Invalid arguments passed to {nameof(ApplicationTitleConverter)}.");

            var serverName = ((Server)value)?.Name;
            return string.IsNullOrEmpty(serverName) ? "Casual Meter" : $"Casual Meter - {serverName}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
