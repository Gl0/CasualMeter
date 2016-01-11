using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.UI;
using CasualMeter.Common.UI.ViewModels;
using Lunyx.Common.UI.Wpf;
using Nicenis.ComponentModel;
using Tera.DamageMeter;

namespace CasualMeter.ViewModels
{
    public class SkillBreakdownViewModel : CasualViewModelBase
    {
        public ThreadSafeObservableCollection<ComboBoxEntity> ComboBoxEntities
        {
            get { return GetProperty<ThreadSafeObservableCollection<ComboBoxEntity>>(); }
            set { SetProperty(value); }
        }

        public ConcurrentDictionary<string, bool> SkillIsAttackDictionary
        {
            get { return GetProperty(getDefault: () => new ConcurrentDictionary<string, bool>()); }
            set { SetProperty(value); }
        }

        public Dictionary<SkillViewType, IList<SortDescription>> SortDescriptionMappings
        {
            get { return GetProperty<Dictionary<SkillViewType, IList<SortDescription>>>(); }
            set { SetProperty(value); }
        }

        public ComboBoxEntity SelectedCollectionView
        {
            get { return GetProperty<ComboBoxEntity>(); }
            set { SetProperty(value, onChanged: OnSelectedViewChanged); }
        }

        public IEnumerable<SortDescription> SortDescriptionSource
        {
            get { return GetProperty<IEnumerable<SortDescription>>(); }
            set { SetProperty(value); }
        }

        private void OnSelectedViewChanged(IPropertyValueChangedEventArgs<ComboBoxEntity> e)
        {
            if (e?.NewValue?.Key == null) return;

            //update the sortdescriptions for this view
            SortDescriptionSource = SortDescriptionMappings[e.NewValue.Key];

            CasualMessenger.Instance.UpdateSkillBreakdownView(this, e.NewValue.Key.ToString());
        }
        
        public PlayerInfo PlayerInfo
        {
            get { return GetProperty<PlayerInfo>(); }
            set { SetProperty(value); }
        }

        public ThreadSafeObservableCollection<SkillResult> SkillLog
        {
            get { return GetProperty<ThreadSafeObservableCollection<SkillResult>>(); }
            set { SetProperty(value); }
        }

        public ThreadSafeObservableCollection<AggregatedSkillResult> AggregatedSkillLogById
        {
            get { return GetProperty<ThreadSafeObservableCollection<AggregatedSkillResult>>(); }
            set { SetProperty(value); }
        }

        public ThreadSafeObservableCollection<AggregatedSkillResult> AggregatedSkillLogByName
        {
            get { return GetProperty<ThreadSafeObservableCollection<AggregatedSkillResult>>(); }
            set { SetProperty(value); }
        }
        
        public SkillBreakdownViewModel(PlayerInfo playerInfo)
        {
            ComboBoxEntities = new ThreadSafeObservableCollection<ComboBoxEntity>
            {
                new ComboBoxEntity(SkillViewType.FlatView, "Flat View"),
                new ComboBoxEntity(SkillViewType.AggregatedSkillIdView, "Aggregate by Id"),
                new ComboBoxEntity(SkillViewType.AggregatedSkillNameView, "Aggregate by Name")
            };

            //NOTE: These are duplicated in the xaml because of a wpf bug
            SortDescriptionMappings = new Dictionary<SkillViewType, IList<SortDescription>>
            {
                {
                    SkillViewType.FlatView,
                    new List<SortDescription>
                    {
                        new SortDescription(nameof(SkillResult.Time), ListSortDirection.Ascending)
                    }

                },
                {
                    SkillViewType.AggregatedSkillIdView,
                    new List<SortDescription>
                    {
                        new SortDescription(nameof(AggregatedSkillResult.Amount), ListSortDirection.Descending)
                    }
                },
                {
                    SkillViewType.AggregatedSkillNameView,
                    new List<SortDescription>
                    {
                        new SortDescription(nameof(AggregatedSkillResult.Amount), ListSortDirection.Descending)
                    }
                }
            };

            //set the intial view
            var initialView = SkillViewType.AggregatedSkillNameView;
            SortDescriptionSource = SortDescriptionMappings[initialView];
            SelectedCollectionView = ComboBoxEntities.First(cbe => cbe.Key == initialView);

            PlayerInfo = playerInfo;
            SkillLog = PlayerInfo.SkillLog;

            //subscribe to future changes and invoke manually
            SkillLog.CollectionChanged += (sender, args) =>
            {
                PopulateAggregatedSkillLogs();
                CasualMessenger.Instance.Messenger.Send(new ScrollPlayerStatsMessage(), this);
            };
            
            PopulateAggregatedSkillLogs();
        }

        private void PopulateAggregatedSkillLogs()
        {
            AggregatedSkillLogById = new ThreadSafeObservableCollection<AggregatedSkillResult>();
            AggregatedSkillLogByName = new ThreadSafeObservableCollection<AggregatedSkillResult>();

            foreach (var skillName in SkillLog.Where(s => !SkillIsAttackDictionary.ContainsKey(s.SkillName)).Select(s => s.SkillName).ToList())
            {
                SkillIsAttackDictionary.GetOrAdd(skillName, false);
            }
            foreach (
                var skill in
                    SkillIsAttackDictionary.Where(kvp => !kvp.Value).ToList()
                        .Where(kvp => SkillLog.Any(s => s.SkillName == kvp.Key && s.Amount > 0))
                        .ToList())
            {
                SkillIsAttackDictionary[skill.Key] = true;
            }
            
            var aggregatedById =(from s in SkillLog.Where(skill => !SkillIsAttackDictionary[skill.SkillName] || skill.Amount > 0)
                                 group s by new { s.SkillId , s.SkillName, s.IsHeal } into grps
                                 select new AggregatedSkillResult
                                 {
                                     DisplayName = grps.FirstOrDefault()?.SkillNameDetailed ?? "Unknown Skill",
                                     Amount = grps.Sum(g => g.Amount),
                                     IsHeal = grps.FirstOrDefault()?.IsHeal,
                                     Hits = grps.Count(),
                                     CritRate = (double)grps.Count(g => g.IsCritical) / grps.Count(),
                                     HighestCrit = grps.Any(g => g.IsCritical) ? grps.Where(g => g.IsCritical).Max(g => g.Amount) : 0,
                                     LowestCrit = grps.Any(g => g.IsCritical) ? grps.Where(g => g.IsCritical).Min(g => g.Amount) : 0,
                                     AverageCrit = grps.Any(g => g.IsCritical) ? Convert.ToInt64(grps.Where(g => g.IsCritical).Average(g => g.Amount)) : 0,
                                     AverageWhite = grps.Any(g => !g.IsCritical) ? Convert.ToInt64(grps.Where(g => !g.IsCritical).Average(g => g.Amount)) : 0,
                                     DamagePercent = (double)grps.Where(g => !g.IsHeal).Sum(g => g.Amount) / SkillLog.Where(s => !s.IsHeal).Sum(s => s.Amount)
                                 }).ToList();

            var aggregatedByName =  (from s in SkillLog.Where(skill => !SkillIsAttackDictionary[skill.SkillName] || skill.Amount > 0)
                                     group s by new { s.SkillName, s.IsHeal } into grps
                                     select new AggregatedSkillResult
                                     {
                                         DisplayName = grps.FirstOrDefault()?.SkillName,
                                         Amount = grps.Sum(g => g.Amount),
                                         IsHeal = grps.FirstOrDefault()?.IsHeal,
                                         Hits = grps.Count(),
                                         CritRate = (double)grps.Count(g => g.IsCritical) / grps.Count(),
                                         HighestCrit = grps.Any(g => g.IsCritical) ? grps.Where(g => g.IsCritical).Max(g => g.Amount) : 0,
                                         LowestCrit = grps.Any(g => g.IsCritical) ? grps.Where(g => g.IsCritical).Min(g => g.Amount) : 0,
                                         AverageCrit = grps.Any(g => g.IsCritical) ? Convert.ToInt64(grps.Where(g => g.IsCritical).Average(g => g.Amount)) : 0,
                                         AverageWhite = grps.Any(g => !g.IsCritical) ? Convert.ToInt64(grps.Where(g => !g.IsCritical).Average(g => g.Amount)) : 0,
                                         DamagePercent = (double)grps.Where(g => !g.IsHeal).Sum(g => g.Amount) / SkillLog.Where(s => !s.IsHeal).Sum(s => s.Amount)
                                     }).ToList();

            FillSkillLog(AggregatedSkillLogById, aggregatedById);
            FillSkillLog(AggregatedSkillLogByName, aggregatedByName);
        }

        private void FillSkillLog(ThreadSafeObservableCollection<AggregatedSkillResult> skillLog,
            IEnumerable<AggregatedSkillResult> results)
        {
            foreach (var result in results)
                skillLog.Add(result);
        }
    }

    public class ComboBoxEntity
    {
        public ComboBoxEntity(SkillViewType key, string value)
        {
            Key = key;
            Value = value;
        }
        public SkillViewType Key { get; set; }
        public string Value { get; set; }
    }

    public enum SkillViewType
    {
        FlatView = 1,
        AggregatedSkillIdView = 2,
        AggregatedSkillNameView = 4
    }
}
