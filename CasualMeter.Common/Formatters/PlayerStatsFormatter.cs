using System;
using System.Collections.Generic;
using System.Linq;
using CasualMeter.Core.Helpers;
using CasualMeter.Tracker;
using Tera.Data;
using Tera.Game.Abnormality;

namespace CasualMeter.Common.Formatters
{
    public class PlayerStatsFormatter : Formatter
    {
        public PlayerStatsFormatter(PlayerInfo playerInfo, TeraData teraData, FormatHelpers formatHelpers)
        {
            var placeHolders = new List<KeyValuePair<string, object>>();
            placeHolders.Add(new KeyValuePair<string, object>("FullName", playerInfo.FullName));
            placeHolders.Add(new KeyValuePair<string, object>("Name", playerInfo.Name));
            placeHolders.Add(new KeyValuePair<string, object>("Class", playerInfo.Class));

            placeHolders.Add(new KeyValuePair<string, object>("Crits", playerInfo.Dealt.Crits));
            placeHolders.Add(new KeyValuePair<string, object>("Hits", playerInfo.Dealt.Hits));

            placeHolders.Add(new KeyValuePair<string, object>("DamagePercent", formatHelpers.FormatPercent(playerInfo.Dealt.DamageFraction) ?? "NaN"));
            placeHolders.Add(new KeyValuePair<string, object>("CritPercent", formatHelpers.FormatPercent((double)playerInfo.Dealt.Crits / playerInfo.Dealt.Hits) ?? "NaN"));

            placeHolders.Add(new KeyValuePair<string, object>("Damage", formatHelpers.FormatValue(playerInfo.Dealt.Damage)));
            placeHolders.Add(new KeyValuePair<string, object>("DamageReceived", formatHelpers.FormatValue(playerInfo.Received.Damage)));
            placeHolders.Add(new KeyValuePair<string, object>("DPS", $"{formatHelpers.FormatValue(SettingsHelper.Instance.Settings.ShowPersonalDps ? playerInfo.Dealt.PersonalDps : playerInfo.Dealt.Dps)}/s"));

            var lastTick = playerInfo.Tracker.LastAttack?.Ticks ?? 0;
            var firstTick = playerInfo.Tracker.FirstAttack?.Ticks ?? 0;
            var slayingstr = "";
            var death = "";
            var deathDur = "";
            if (lastTick > firstTick && firstTick > 0)
            {
                var buffs = playerInfo.Tracker.Abnormals.Get(playerInfo.Player);
                AbnormalityDuration slaying;
                buffs.Times.TryGetValue(teraData.HotDotDatabase.Get(8888889), out slaying);
                double slayingperc = (double) (slaying?.Duration(firstTick, lastTick) ?? 0)/(lastTick - firstTick);
                slayingstr = formatHelpers.FormatPercent(slayingperc);
                death = buffs.Death.Count(firstTick, lastTick).ToString();
                deathDur = formatHelpers.FormatTimeSpan(TimeSpan.FromTicks(buffs.Death.Duration(firstTick, lastTick)));
            }
            placeHolders.Add(new KeyValuePair<string, object>("Death", death));
            placeHolders.Add(new KeyValuePair<string, object>("DeathDuration", deathDur));
            placeHolders.Add(new KeyValuePair<string, object>("Slaying", slayingstr));

            Placeholders = placeHolders.ToDictionary(x => x.Key, y => y.Value);
            FormatProvider = formatHelpers.CultureInfo;
        }
    }
}
