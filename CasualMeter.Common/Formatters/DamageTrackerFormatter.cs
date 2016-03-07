using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CasualMeter.Common.Helpers;
using Tera.DamageMeter;

namespace CasualMeter.Common.Formatters
{
    public class DamageTrackerFormatter : Formatter
    {
        public DamageTrackerFormatter(DamageTracker damageTracker, FormatHelpers formatHelpers)
        {
            var placeHolders = new List<KeyValuePair<string, object>>();
            placeHolders.Add(new KeyValuePair<string, object>("Boss", damageTracker.Name??string.Empty));
            placeHolders.Add(new KeyValuePair<string, object>("Time", formatHelpers.FormatTimeSpan(damageTracker.Duration)));

            Placeholders = placeHolders.ToDictionary(x => x.Key, y => y.Value);
            FormatProvider = formatHelpers.CultureInfo;
        }
    }
}
