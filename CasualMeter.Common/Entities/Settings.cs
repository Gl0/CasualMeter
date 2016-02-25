using CasualMeter.Common.JsonConverters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CasualMeter.Common.Entities
{
    public class Settings : DefaultValueEntity
    {
        [DefaultValue(100)]
        public double WindowLeft { get; set; }

        [DefaultValue(100)]
        public double WindowTop { get; set; }

        [DefaultValue(1)]
        public double Opacity { get; set; }

        [DefaultValue(1)]
        public double UiScale { get; set; }

        [DefaultValue(true)]
        public bool IsPinned { get; set; }

        [DefaultValue(false)]
        public bool UseCompactView { get; set; }

        [DefaultValue(false)]
        public bool ShowPersonalDps { get; set; }

        [DefaultValue("{Name} {Damage} {DPS} {DamagePercent}")]
        public string DpsPasteFormat { get; set; }

        [DefaultValue(30)]
        public int InactivityResetDuration { get; set; }

        [DefaultValue(5)]
        public int ExpandedViewPlayerLimit { get; set; }

        [DefaultValue(false)]
        public bool UseGlobalHotkeys { get; set; }

        [DefaultValue(false)]
        public bool OnlyBosses { get; set; }

        [DefaultValue(true)]
        public bool IgnoreOneshots { get; set; }

        [DefaultValue(false)]
        public bool UseRawSockets { get; set; }

        [JsonConverter(typeof(LanguageConverter))]
        [DefaultValue("Auto")]
        public string Language { get; set; }

        //since you can't set DefaultValueAttribute on objects
        private HotKeySettings _hotkeys;
        public HotKeySettings HotKeys
        {
            get { return _hotkeys ?? (_hotkeys = new HotKeySettings()); }
            set { _hotkeys = value; }
        }
    }
}
