// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using log4net;
using Lunyx.Common.UI.Wpf;
using Nicenis.ComponentModel;
using Tera.Game;

namespace Tera.DamageMeter
{
    public class DamageTracker : PropertyObservable
    {
        private static readonly ILog Logger = LogManager.GetLogger
            (MethodBase.GetCurrentMethod().DeclaringType);

        public ThreadSafeObservableCollection<PlayerInfo> StatsByUser
        {
            get { return GetProperty(getDefault: () => new ThreadSafeObservableCollection<PlayerInfo>()); }
            set { SetProperty(value); }
        }

        public bool OnlyBosses {
            get { return GetProperty<bool>(getDefault: () => false); }
            set { SetProperty(value); }
        }

        public bool IgnoreOneshots
        {
            get { return GetProperty<bool>(getDefault: () => true); }
            set { SetProperty(value); }
        }

        public bool IsArchived
        {
            get { return GetProperty<bool>(getDefault: () => false); }
            set { SetProperty(value); }
        }

        public DateTime? FirstAttack
        {
            get { return GetProperty<DateTime?>(); }
            set { SetProperty(value, 
                onChanged: e =>
                {
                    if (LastAttack != null && e.NewValue != null) Duration = (DateTime) LastAttack - (DateTime) e.NewValue;
                });
            }
        }

        public DateTime? LastAttack
        {
            get { return GetProperty<DateTime?>(); }
            set
            {
                SetProperty(value,
                    onChanged: e =>
                    {
                        if (e.NewValue != null && FirstAttack != null) Duration = (DateTime)e.NewValue - (DateTime)FirstAttack;
                    });
            }
        }

        public TimeSpan Duration
        {
            get { return GetProperty<TimeSpan>(getDefault: () => TimeSpan.Zero); }
            set { SetProperty(value); }
        }

        public SkillStats TotalDealt
        {
            get { return GetProperty(getDefault: () => new SkillStats()); }
            set { SetProperty(value); }
        }

        private PlayerInfo GetOrCreate(SkillResult skillResult)
        {
            NpcEntity npctarget = skillResult.Target as NpcEntity;
            if (npctarget != null)
            {
                if (OnlyBosses)//not count bosses
                    if (!npctarget.Info.Boss)
                        return null;
                if (IgnoreOneshots)//ignore damage that is more than 10x times than mob's hp
                    if ((npctarget.Info.HP>0) && (npctarget.Info.HP <= skillResult.Damage/10))
                        return null;
            }
            var player = skillResult.SourcePlayer;
            PlayerInfo playerStats = StatsByUser.FirstOrDefault(pi => pi.Player.Equals(player));
            if (playerStats == null && (IsFromHealer(skillResult) ||//either healer
               (!IsFromHealer(skillResult) && IsValidAttack(skillResult))))//or damage from non-healer
            {
                playerStats = new PlayerInfo(player, this);
                StatsByUser.Add(playerStats);
            }
            return playerStats;
        }

        public void Update(SkillResult skillResult)
        {
            if (IsArchived) return;//prevent archived trackers from accidentally recording stats

            if (skillResult.SourcePlayer != null)
            {
                var playerStats = GetOrCreate(skillResult);
                if (playerStats == null) return; //if this is null, that means we should ignore it
                var statsChange = StatsChange(skillResult);
                if (statsChange == null) Logger.Warn($"Generated null SkillStats from {skillResult}");

                playerStats.LogSkillUsage(skillResult);

                TotalDealt.Add(statsChange);
                playerStats.Dealt.Add(statsChange);
            }

            if (IsValidAttack(skillResult))
            {
                if (FirstAttack == null)
                    FirstAttack = skillResult.Time;

                LastAttack = skillResult.Time;
            }

            foreach (var playerStat in StatsByUser)
            {   //force update of calculated dps metrics
                playerStat.Dealt.UpdateStats();
            }
        }

        public bool IsFromHealer(SkillResult skillResult)
        {
            return skillResult.SourcePlayer.IsHealer;
        }

        public bool IsValidAttack(SkillResult skillResult)
        {
            return skillResult.SourcePlayer != null && (skillResult.Damage > 0) &&
                   (skillResult.Source.Id != skillResult.Target.Id);
        }

        private SkillStats StatsChange(SkillResult message)
        {
            var result = new SkillStats();
            if (message.Amount == 0)
                return result;
            
            result.Damage = message.Damage;
            result.Heal = message.Heal;

            if (IsFromHealer(message) || (!IsFromHealer(message) && !message.IsHeal))
            {
                result.Hits++;
                if (message.IsCritical)
                    result.Crits++;
            }
            
            return result;
        }

        public long CalculateDps(long damage)
        {
            return CalculateDps(damage, Duration);
        }

        public long CalculateDps(long damage, TimeSpan duration)
        {
            var durationInSeconds = duration.TotalSeconds;
            if (durationInSeconds < 1)
                durationInSeconds = 1;
            var dps = damage / durationInSeconds;
            return (long)dps;
        }
        public string Name { get; set; }
    }
}
