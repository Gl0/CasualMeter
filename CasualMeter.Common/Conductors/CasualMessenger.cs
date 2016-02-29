using System;
using CasualMeter.Common.Conductors.Messages;
using GalaSoft.MvvmLight.Messaging;
using Lunyx.Common.UI.Wpf;
using Tera.DamageMeter;
using Tera.Game;

namespace CasualMeter.Common.Conductors
{
    public sealed class CasualMessenger
    {
        private static readonly Lazy<CasualMessenger> Lazy = new Lazy<CasualMessenger>(() => new CasualMessenger());

        public static CasualMessenger Instance => Lazy.Value;

        public IMessenger Messenger { get { return GalaSoft.MvvmLight.Messaging.Messenger.Default; } }

        private CasualMessenger()
        {
        }

        public void ResetPlayerStats(bool shouldSaveCurrent)
        {
            Messenger.Send(new ResetPlayerStatsMessage
            {
                ShouldSaveCurrent = shouldSaveCurrent
            });
        }

        public void PastePlayerStats()
        {
            Messenger.Send(new PastePlayerStatsMessage());
        }

        public void RefreshVisibility(bool? isVisible, bool toggle)
        {
            Messenger.Send(new RefreshVisibilityMessage
            {
                IsVisible = isVisible,
                Toggle = toggle
            });
        }

        public void UpdateSkillBreakdownView(object sender, string viewKey)
        {
            Messenger.Send(new UpdateSkillBreakdownViewMessage
            {
                ViewKey = viewKey
            }, sender);
        }

    }
}
