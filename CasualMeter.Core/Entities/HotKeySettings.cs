using System.ComponentModel;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CasualMeter.Core.Entities
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
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierUpload { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(ModifierKeys.Control)]
        public ModifierKeys ModifierDetails { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.Insert)]
        public Key Paste { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.Delete)]
        public Key Reset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.End)]
        public Key Save { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.PageUp)]
        public Key Upload { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(Key.PageDown)]
        public Key Details { get; set; }
    }
}
