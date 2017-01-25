using System;
using System.Globalization;
using System.Windows.Data;
using CasualMeter.Core.Helpers;
using Tera.Game;

namespace CasualMeter.Converters
{
    public class PlayerClassToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is PlayerClass))
                throw new ArgumentException($"Invalid arguments passed to {nameof(PlayerClassToImageConverter)}.");

            return SettingsHelper.Instance.GetImage((PlayerClass) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
