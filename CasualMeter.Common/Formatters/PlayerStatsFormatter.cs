using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CasualMeter.Common.Helpers;
using Tera.DamageMeter;

namespace CasualMeter.Common.Formatters
{
    public class PlayerStatsFormatter : Formatter
    {
        public PlayerStatsFormatter(PlayerInfo playerInfo, FormatHelpers formatHelpers)
        {
            var placeHolders = new List<KeyValuePair<string, object>>();
            placeHolders.Add(new KeyValuePair<string, object>("Name", playerInfo.Name));
            placeHolders.Add(new KeyValuePair<string, object>("FullName", playerInfo.FullName));
            placeHolders.Add(new KeyValuePair<string, object>("Class", playerInfo.Class));

            placeHolders.Add(new KeyValuePair<string, object>("Crits", playerInfo.Dealt.Crits));
            placeHolders.Add(new KeyValuePair<string, object>("Hits", playerInfo.Dealt.Hits));

            placeHolders.Add(new KeyValuePair<string, object>("DamagePercent", formatHelpers.FormatPercent(playerInfo.Dealt.DamageFraction) ?? "NaN"));
            placeHolders.Add(new KeyValuePair<string, object>("CritPercent", formatHelpers.FormatPercent((double)playerInfo.Dealt.Crits / playerInfo.Dealt.Hits) ?? "NaN"));

            placeHolders.Add(new KeyValuePair<string, object>("Damage", formatHelpers.FormatValue(playerInfo.Dealt.Damage)));
            placeHolders.Add(new KeyValuePair<string, object>("DamageReceived", formatHelpers.FormatValue(playerInfo.Received.Damage)));
            placeHolders.Add(new KeyValuePair<string, object>("DPS", $"{formatHelpers.FormatValue(SettingsHelper.Instance.Settings.ShowPersonalDps ? playerInfo.Dealt.PersonalDps : playerInfo.Dealt.Dps)}/s"));

            Placeholders = placeHolders.ToDictionary(x => x.Key, y => y.Value);
            FormatProvider = formatHelpers.CultureInfo;
        }
    }
}
