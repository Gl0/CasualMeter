using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CasualMeter.Common.Conductors;
using CasualMeter.Common.Conductors.Messages;
using CasualMeter.Common.Helpers;
using CasualMeter.Common.UI.ViewModels;
using Lunyx.Common.UI.Wpf.Controls;

namespace CasualMeter.Common.UI.Controls
{
    public class CasualMeterWindow : ClickThroughWindow
    {
        public CasualViewModelBase ViewModel { get; set; }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            //register visibility messages and set initial visiblity
            CasualMessenger.Instance.Messenger.Register<RefreshVisibilityMessage>(this, SetVisibility);
            ProcessHelper.Instance.ForceVisibilityRefresh();

            Topmost = true;
            ShowInTaskbar = false;
            Height = System.Windows.Forms.Screen.AllScreens.Max(x=>x.Bounds.Height);// Fix clipping window on large monitors
            ResizeMode = ResizeMode.NoResize;
            WindowStyle= WindowStyle.None;
            AllowsTransparency = true;
            Background = null;
                            
            //set cursor
            Cursor = new Cursor(new MemoryStream(Properties.Resources.Arrow));
        }

        /// <summary>
        /// Attach to MouseDown event of controls to allow moving of window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void MoveControl(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void SetVisibility(RefreshVisibilityMessage message)
        {
            Topmost = false;
            Topmost = true;// sometimes disappears, need refresh
            if (SettingsHelper.Instance.Settings.IsPinned)
            {
                Visibility = Visibility.Visible;
                return;
            }
            // at this point, it means that we are not pinned, so set the visibility accordingly
            if (message.IsVisible == null) return; //if this is null, there was an error getting active process
            Visibility = message.IsVisible.Value ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //this code should stay here because visibility sometimes gets toggled 
            //before window called OnClosed, but already is in an invalid state

            //unregister messages
            CasualMessenger.Instance.Messenger.Unregister(ViewModel);
            CasualMessenger.Instance.Messenger.Unregister(this);

            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel = null;
            base.OnClosed(e);
        }
    }
}
