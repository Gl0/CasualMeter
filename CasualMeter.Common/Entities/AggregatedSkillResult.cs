using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Lunyx.Common.UI.Wpf;
using Nicenis.ComponentModel;
using Tera.Game;

namespace CasualMeter.Common.Entities
{
    public enum AggregationType
    {
        Id,
        Name
    }

    public static class SkillResultExtensions
    {
        private static List<string> Lvls = new List<string> { " I", " II", " III", " IV", " V", " VI", " VII", " VIII", " IX", " X",
            " XI", " XII", " XIII", " XIV", " XV", " XVI", " XVII", " XVIII", " XIX", " XX"};

        public static string RemoveLvl(string name)
        {
            var res = name;
            foreach (string lvl in Lvls)
            {
                if (res.EndsWith(lvl) || res.Contains(lvl + " "))
                {
                    res = res.Replace(lvl, "");
                    break;
                }
            }
            return res;
        }
        public static bool IsSameSkillAs(this SkillResult skillResult, SkillResult other, AggregationType type)
        {
            return skillResult.AggregatedSkillName(type) == other.AggregatedSkillName(type) &&
                   skillResult.IsHeal == other.IsHeal;
        }

        public static bool IsSameSkillAs(this SkillResult skillResult, AggregatedSkillResult aggregatedSkillResult)
        {
            return skillResult.AggregatedSkillName(aggregatedSkillResult.AggregationType) ==
                   aggregatedSkillResult.DisplayName &&
                   skillResult.IsHeal == aggregatedSkillResult.IsHeal;
        }

        private static string AggregatedSkillName(this SkillResult skillResult, AggregationType type)
        {
            switch (type)
            {
                case AggregationType.Id:
                    return skillResult.SkillNameDetailed;
                case AggregationType.Name:
                    return RemoveLvl(skillResult.SkillName);
                default:
                    return string.Empty;
            }
        }
    }

    public class AggregatedSkillResult : PropertyObservable
    {
        public AggregatedSkillResult(string displayName, bool isHeal, AggregationType type, ThreadSafeObservableCollection<SkillResult> skillLog)
        {
            DisplayName = type==AggregationType.Name ? SkillResultExtensions.RemoveLvl(displayName) : displayName;
            IsHeal = isHeal;
            AggregationType = type;
            SkillLog = skillLog;
            SkillLog.CollectionChanged += SkillLog_CollectionChanged;
        }

        private void SkillLog_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems.Cast<SkillResult>().Any(sr => sr.IsSameSkillAs(this)))
            {
                OnPropertyChanged(nameof(Amount));
                OnPropertyChanged(nameof(Hits));
                OnPropertyChanged(nameof(CritRate));
                OnPropertyChanged(nameof(HighestCrit));
                OnPropertyChanged(nameof(LowestCrit));
                OnPropertyChanged(nameof(AverageCrit));
                OnPropertyChanged(nameof(AverageWhite));
                OnPropertyChanged(nameof(DamagePercent));
            }
        }

        private ThreadSafeObservableCollection<SkillResult> SkillLog
        {
            get { return GetProperty<ThreadSafeObservableCollection<SkillResult>>(); }
            set { SetProperty(value); }
        }

        private IEnumerable<SkillResult> FilteredSkillLog =>
            from skill in SkillLog
            where (skill.IsSameSkillAs(this) && skill.Amount > 0) ||
                  (skill.IsSameSkillAs(this) && skill.Amount == 0 && !SkillLog.Any(s => s.IsSameSkillAs(skill, AggregationType) && s.Amount != 0))
            select skill;

        public AggregationType AggregationType { get; }
        public string DisplayName { get; }
        public bool IsHeal { get; }
        public long Amount => FilteredSkillLog.Sum(s => s.Amount);
        public long Damage => FilteredSkillLog.Sum(s => s.Damage);
        public int Hits => FilteredSkillLog.Count();
        public int SkillId => FilteredSkillLog.Count() > 0 ? FilteredSkillLog.ElementAt(0).SkillId : 0;
        public NpcInfo NpcInfo => FilteredSkillLog.Count() > 0 ? FilteredSkillLog.ElementAt(0).Skill?.NpcInfo:null;
        public double CritRate => (double) FilteredSkillLog.Count(g => g.IsCritical)/FilteredSkillLog.Count();
        public long HighestCrit => FilteredSkillLog.Any(g => g.IsCritical) ? FilteredSkillLog.Where(g => g.IsCritical).Max(g => g.Amount) : 0;
        public long LowestCrit => FilteredSkillLog.Any(g => g.IsCritical) ? FilteredSkillLog.Where(g => g.IsCritical).Min(g => g.Amount) : 0;
        public long AverageCrit => FilteredSkillLog.Any(g => g.IsCritical) ? Convert.ToInt64(FilteredSkillLog.Where(g => g.IsCritical).Average(g => g.Amount)) : 0;
        public long AverageWhite => FilteredSkillLog.Any(g => !g.IsCritical) ? Convert.ToInt64(FilteredSkillLog.Where(g => !g.IsCritical).Average(g => g.Amount)) : 0;
        public double DamagePercent => (double)FilteredSkillLog.Where(g => !g.IsHeal).Sum(g => g.Amount) / SkillLog.Where(s => !s.IsHeal).Sum(s => s.Amount);
    }
}
