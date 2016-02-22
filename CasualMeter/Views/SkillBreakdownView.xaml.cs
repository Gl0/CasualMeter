using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Converters;
using CasualMeter.ViewModels;
using Lunyx.Common.UI.Wpf.Controls;
using Tera.DamageMeter;

namespace CasualMeter.Views
{
    /// <summary>
    /// Interaction logic for SkillBreakdownView.xaml
    /// </summary>
    public partial class SkillBreakdownView
    {
        public SkillBreakdownViewModel SkillBreakdownViewModel => ViewModel as SkillBreakdownViewModel;
        private readonly LongToStringConverter _longToStringConverter = new LongToStringConverter();
        private readonly DoubleToPercentStringConverter _doubleToPercentStringConverter = new DoubleToPercentStringConverter();
        private readonly DateTimeToTimeSpanStringConverter _dateTimeToTimeSpanStringConverter = new DateTimeToTimeSpanStringConverter();

        public SkillBreakdownView(SkillBreakdownViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            DataContext = ViewModel;

            //manually switch first view, then register for all future updates
            SwitchView(SkillBreakdownViewModel?.SelectedCollectionView.Key.ToString());
            CasualMessenger.Instance.Messenger.Register<UpdateSkillBreakdownViewMessage>(this, ViewModel,
                UpdateSkillBreakdownView);
            CasualMessenger.Instance.Messenger.Register<ScrollPlayerStatsMessage>(this, ViewModel,
                ScrollPlayerStats);

            base.OnInitialized(e);
        }

        private void ScrollPlayerStats(ScrollPlayerStatsMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                if (SkillBreakdownViewModel?.SelectedCollectionView.Key == SkillViewType.FlatView)
                {
                    if (SkillResultsGrid.Items.Count > 0)
                    {
                        var border = VisualTreeHelper.GetChild(SkillResultsGrid, 0) as Decorator;
                        if (border != null)
                        {
                            var scroll = border.Child as ScrollViewer;
                            scroll?.ScrollToEnd();
                        }
                    }
                }
            });
        }

        private void UpdateSkillBreakdownView(UpdateSkillBreakdownViewMessage message)
        {
            Dispatcher.Invoke(() => SwitchView(message.ViewKey));
        }

        private void SwitchView(string viewKey)
        {
            var resource = LayoutGrid.Resources[viewKey];

            var view = resource as CollectionViewSource;
            if (view == null) return;

            SkillResultsGrid.Columns.Clear();
            SkillResultsGrid.DataContext = view;
            switch (viewKey)
            {   //assign columns
                case nameof(SkillViewType.FlatView):
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Time",
                        Binding = new Binding(nameof(SkillResult.Time))
                        {
                            Converter = _dateTimeToTimeSpanStringConverter,
                            ConverterParameter = SkillBreakdownViewModel.PlayerInfo.EncounterStartTime
                        }
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Skill Id",
                        Binding = new Binding(nameof(SkillResult.SkillId))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Skill Name",
                        Binding = new Binding(nameof(SkillResult.SkillNameDetailed))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Amount",
                        Binding = new Binding(nameof(SkillResult.Amount))
                        {
                            Converter = _longToStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(SkillResult.Amount))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Is Crit?",
                        Binding = new Binding(nameof(SkillResult.IsCritical))
                    });
                    //SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    //{
                    //    Header = "Is Chained?",
                    //    Binding = new Binding(nameof(SkillResult.IsChained))
                    //});
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Is Heal?",
                        Binding = new Binding(nameof(SkillResult.IsHeal))
                    });
                    break;
                case nameof(SkillViewType.AggregatedSkillIdView):
                case nameof(SkillViewType.AggregatedSkillNameView):
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Skill",
                        Binding = new Binding(nameof(AggregatedSkillResult.DisplayName))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Hits",
                        Binding = new Binding(nameof(AggregatedSkillResult.Hits))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Amount",
                        Binding = new Binding(nameof(AggregatedSkillResult.Amount))
                        {
                            Converter = _longToStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.Amount))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Crit Rate",
                        Binding = new Binding(nameof(AggregatedSkillResult.CritRate))
                        {
                            Converter = _doubleToPercentStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.CritRate))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Damage Percent",
                        Binding = new Binding(nameof(AggregatedSkillResult.DamagePercent))
                        {
                            Converter = _doubleToPercentStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.DamagePercent))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Is Heal?",
                        Binding = new Binding(nameof(AggregatedSkillResult.IsHeal))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Highest Crit",
                        Binding = new Binding(nameof(AggregatedSkillResult.HighestCrit))
                        {
                            Converter = _longToStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.HighestCrit))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Lowest Crit",
                        Binding = new Binding(nameof(AggregatedSkillResult.LowestCrit))
                        {
                            Converter = _longToStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.LowestCrit))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Average Crit",
                        Binding = new Binding(nameof(AggregatedSkillResult.AverageCrit))
                        {
                            Converter = _longToStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.AverageCrit))
                    });
                    SkillResultsGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Average White",
                        Binding = new Binding(nameof(AggregatedSkillResult.AverageWhite))
                        {
                            Converter = _longToStringConverter
                        },
                        ClipboardContentBinding = new Binding(nameof(AggregatedSkillResult.AverageWhite))
                    });
                    break;
            }
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel = null;
            Close();
        }

        private void SkillResultsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {   
            //copy to clipboard
            try
            {
                ApplicationCommands.Copy.Execute(null, (SortableDataGrid) sender);
            }
            catch (Exception) { /* eat it */ }
        }
    }
}
