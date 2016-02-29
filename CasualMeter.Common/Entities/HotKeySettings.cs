using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CasualMeter.Common.Entities
{
    public class HotKeySettings : DefaultValueEntity
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierPaste { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierReset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierSave { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.Insert)]
        public Key Paste { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.Delete)]
        public Key Reset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.End)]
        public Key Save { get; set; }
    }
}
