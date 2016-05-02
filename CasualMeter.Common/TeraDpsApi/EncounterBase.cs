using System.Collections.Generic;

namespace CasualMeter.Common.TeraDpsApi
{
    public class EncounterBase
    {

        public string areaId;
        public string bossId;
        public string fightDuration;
        public string meterName =  "CasualMeter";
        public string meterVersion = "test";
        public string partyDps;
        public List<KeyValuePair<string, string>> debuffUptime = new List<KeyValuePair<string, string>>();
        public List<Members> members = new List<Members>();

    }
}
