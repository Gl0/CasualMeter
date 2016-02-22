using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Helpers;
using CasualMeter.Common.UI.Controls;
using CasualMeter.ViewModels;
using CasualMeter.Views;
using Lunyx.Common;
using Lunyx.Common.UI.Wpf.Extensions;
using Tera.DamageMeter;
using System.Windows.Forms;

namespace CasualMeter
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView
    {
        public ShellViewModel ShellViewModel => ViewModel as ShellViewModel;

        public ShellView()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            //ensure initialization of helpers
            SettingsHelper.Instance.Initialize();
            HotkeyHelper.Instance.Initialize();
            ProcessHelper.Instance.Initialize();

            //initialize viewmodel
            DataContext = ViewModel = new ShellViewModel();
            ShellViewModel.Initialize();

            //load settings
            Left = SettingsHelper.Instance.Settings.WindowLeft;
            Top = SettingsHelper.Instance.Settings.WindowTop;
            OpacityScaleSlider.Value = SettingsHelper.Instance.Settings.Opacity;
            UiScaleSlider.Value = SettingsHelper.Instance.Settings.UiScale;
            ShellViewModel.IsPinned = SettingsHelper.Instance.Settings.IsPinned;
            ShellViewModel.OnlyBosses = SettingsHelper.Instance.Settings.OnlyBosses;
            ShellViewModel.IgnoreOneshots = SettingsHelper.Instance.Settings.IgnoreOneshots;
            ProcessHelper.Instance.UpdateHotKeys();

            base.OnInitialized(e);
        }

        private void SaveUiSettings()
        {
            SettingsHelper.Instance.Settings.WindowLeft = Left;
            SettingsHelper.Instance.Settings.WindowTop = Top;
            SettingsHelper.Instance.Settings.Opacity = OpacityScaleSlider.Value;
            SettingsHelper.Instance.Settings.UiScale = UiScaleSlider.Value;
            SettingsHelper.Instance.Save();
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            SaveUiSettings();
            Close();
        }

        private void SettingsButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            SaveUiSettings();
        }

        private void SkillInfo_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                var control = sender as PlayerInfoControl;
                var playerInfo = control?.PlayerInfo;
                if (playerInfo != null)
                {
                    var vm = new SkillBreakdownViewModel(playerInfo);
                    var v = new SkillBreakdownView(vm)
                    {
                        Owner = this
                    };
                    var headerHeight = 27;//approximate height of the title bar on the skill breakdown window
                    var ownedWindows = OwnedWindows.Cast<Window>().Where(w => w.IsVisible).ToList();
                    if (!ownedWindows.Any())
                    {
                        //we should move away from windows form here if possible.
                        Screen screen = Screen.FromHandle(new WindowInteropHelper(this).Handle);
                        // Transform screen point to WPF device independent point
                        PresentationSource source = PresentationSource.FromVisual(this);

                        if (source?.CompositionTarget == null)
                        {   //if this can't be determined, just use the center screen logic
                            v.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        }
                        else
                        {
                            // WindowStartupLocation.CenterScreen sometimes put window out of screen in multi monitor environment
                            v.WindowStartupLocation = WindowStartupLocation.Manual;
                            Matrix m = source.CompositionTarget.TransformToDevice;
                            double dx = m.M11;
                            double dy = m.M22;
                            Point locationFromScreen = new Point(
                                screen.Bounds.X + (screen.Bounds.Width - v.Width * dx) / 2,
                                screen.Bounds.Y + (screen.Bounds.Height - (v.SkillResultsGridContainer.MaxHeight + headerHeight) * dy) / 2);
                            Point targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
                            v.Left = targetPoints.X;
                            v.Top = targetPoints.Y;
                        }
                    }
                    else
                    {
                        v.WindowStartupLocation = WindowStartupLocation.Manual;
                        v.Left = ownedWindows.Max(w => w.Left) + headerHeight;
                        v.Top = ownedWindows.Max(w => w.Top) + headerHeight;
                    }
                    v.Show();
                }
            }
        }

        #region Handle System Events

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            switch (msg)
            {
                case WM_POWERBROADCAST:
                    switch (wparam.ToInt32())
                    {
                        //value passed when system is resumed after suspension.
                        case PBT_APMRESUMESUSPEND:
                            //reinitialize when resuming
                            ShellViewModel.Initialize();
                            break;

                        //value passed when system is going on standby / suspended
                        case PBT_APMQUERYSUSPEND:
                        //value passed when system Suspend Failed
                        case (PBT_APMQUERYSUSPENDFAILED):
                        //value passed when system is suspended
                        case (PBT_APMSUSPEND):
                        //value passed when system is resumed automatically
                        case (PBT_APMRESUMEAUTOMATIC):
                        //value passed when system is resumed from critical suspension possibly caused by a battery failure
                        case (PBT_APMRESUMECRITICAL):
                        //value passed when system is low on battery
                        case (PBT_APMBATTERYLOW):
                        //value passed when system power status changed from battery to AC power or vice-a-versa
                        case (PBT_APMPOWERSTATUSCHANGE):
                        //value passed when OEM Event is fired. Not sure what that is??
                        case (PBT_APMOEMEVENT):
                            break;
                    }
                    break;
                default:
                    break;
            }

            return IntPtr.Zero;
        }

        //Windows Constants
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_APMQUERYSUSPEND = 0x0000;
        private const int PBT_APMRESUMESUSPEND = 0x0007;
        private const int PBT_APMQUERYSTANDBY = 0x0001;
        private const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        private const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        private const int PBT_APMSUSPEND = 0x0004;
        private const int PBT_APMSTANDBY = 0x0005;
        private const int PBT_APMRESUMECRITICAL = 0x0006;
        private const int PBT_APMRESUMESTANDBY = 0x0008;
        private const int PBTF_APMRESUMEFROMFAILURE = 0x00000001;
        private const int PBT_APMBATTERYLOW = 0x0009;
        private const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
        private const int PBT_APMOEMEVENT = 0x000B;
        private const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        #endregion
    }
}
