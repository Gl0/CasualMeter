using System;
using System.Collections.Generic;
using CasualMeter.Common.Helpers;

namespace CasualMeter.Common.TeraDpsApi
{
    public class EncounterBase
    {
        public DateTime encounterDateTime;
        public string areaId;
        public string bossId;
        public string fightDuration;
        public string meterName =  "CasualMeter";
        public string meterVersion = SettingsHelper.Instance.Version;
        public string partyDps;
        public List<KeyValuePair<string, string>> debuffUptime = new List<KeyValuePair<string, string>>();
        public List<Members> members = new List<Members>();

    }
}
