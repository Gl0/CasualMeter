// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CasualMeter.Core.Helpers;
using Lunyx.Common.UI.Wpf.Collections;
using Nicenis.ComponentModel;
using Tera.Game;

namespace CasualMeter.Tracker
{
    public class PlayerInfo : PropertyObservable
    {
        public DamageTracker Tracker { get; private set; }

        public Player Player { get; private set; }

        public string Name => Player.Name;
        public string FullName => Player.FullName;
        public PlayerClass Class => Player.Class;

        public SynchronizedObservableCollection<SkillResult> SkillLog { get; private set; }

        public DateTime EncounterStartTime => Tracker.FirstAttack ?? DateTime.Now;

        public SkillStats Received { get; private set; }
        public SkillStats Dealt { get; private set; }

        public PlayerInfo(Player user, DamageTracker tracker)
        {
            Tracker = tracker;
            Player = user;
            SkillLog = new SynchronizedObservableCollection<SkillResult>();

            Received = new SkillStats(tracker, SkillLog);
            Dealt = new SkillStats(tracker, SkillLog);
        }

        public void LogSkillUsage(SkillResult result)
        {
            SkillLog.Add(result);
        }

        public override bool Equals(object obj)
        {
            var other = obj as PlayerInfo;
            return Player.PlayerId.Equals(other?.Player.PlayerId);
        }

        public override int GetHashCode()
        {
            return Player.PlayerId.GetHashCode();
        }
    }
}
