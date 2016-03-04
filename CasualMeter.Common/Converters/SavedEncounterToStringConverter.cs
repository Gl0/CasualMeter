using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Tera.DamageMeter;

namespace CasualMeter.Common.Converters
{
    public class SavedEncounterToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value != null && !(value is DamageTracker)) || value == null)
                throw new ArgumentException($"Invalid arguments passed to {nameof(SavedEncounterToStringConverter)}.");
            
            var tracker = (DamageTracker) value;
            if (tracker.FirstAttack == null) throw new ArgumentNullException($"FirstAttack should never be null in a saved encounter.");

            var formatHelper = FormatHelpers.Pretty;
            return $"{tracker.Name}{tracker.FirstAttack.Value.ToLocalTime().ToString("T")} | {formatHelper.FormatValue(tracker.TotalDealt.Damage)} | {formatHelper.FormatValue(tracker.CalculateDps(tracker.TotalDealt.Damage))}/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
