using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CasualMeter.Common;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Entities;
using CasualMeter.Common.UI;
using CasualMeter.Common.UI.ViewModels;
using Lunyx.Common.UI.Wpf;
using Nicenis.ComponentModel;
using Tera.DamageMeter;
using Tera.Game;

namespace CasualMeter.ViewModels
{
    public class SkillBreakdownViewModel : CasualViewModelBase
    {
        public ThreadSafeObservableCollection<ComboBoxEntity> ComboBoxEntities
        {
            get { return GetProperty<ThreadSafeObservableCollection<ComboBoxEntity>>(); }
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
            get { return GetProperty(getDefault: () => new ThreadSafeObservableCollection<AggregatedSkillResult>()); }
            set { SetProperty(value); }
        }

        public ThreadSafeObservableCollection<AggregatedSkillResult> AggregatedSkillLogByName
        {
            get { return GetProperty(getDefault: () => new ThreadSafeObservableCollection<AggregatedSkillResult>()); }
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
                UpdateAggregatedSkillLogs(args.NewItems.Cast<SkillResult>());
                CasualMessenger.Instance.Messenger.Send(new ScrollPlayerStatsMessage(), this);
            };
            
            UpdateAggregatedSkillLogs(SkillLog);
        }

        private void UpdateAggregatedSkillLogs(IEnumerable<SkillResult> newSkillResults)
        {
            foreach (var skillResult in newSkillResults)
            {
                if (AggregatedSkillLogById.All(asr => !skillResult.IsSameSkillAs(asr)))
                {
                    AggregatedSkillLogById.Add(new AggregatedSkillResult(skillResult.SkillNameDetailed,
                        skillResult.IsHeal, AggregationType.Id, SkillLog));
                }
                if (AggregatedSkillLogByName.All(asr => !skillResult.IsSameSkillAs(asr)))
                {
                    AggregatedSkillLogByName.Add(new AggregatedSkillResult(skillResult.SkillShortName, 
                        skillResult.IsHeal, AggregationType.Name, SkillLog));
                }
            }
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
