using System;
using System.Collections.Generic;
using CasualMeter.Core.Helpers;

namespace CasualMeter.Common.TeraDpsApi
{
    public class EncounterBase
    {
        public double encounterUnixEpoch;
        public string areaId;
        public string bossId;
        public string fightDuration;
        public string meterName =  "CasualMeter";
        public string meterVersion = SettingsHelper.Instance.Version;
        public string partyDps;
        public string uploader; //zero-based index of uploader in members list
        public List<List<object>> debuffDetail = new List<List<object>>();
        public List<Members> members = new List<Members>();

    }
}
