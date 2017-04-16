using System.Collections.Generic;

namespace CasualMeter.Common.TeraDpsApi
{
    public class Members
    {


        public string playerServer;
        public string playerDeaths;
        public string playerDeathDuration;
        public string aggro;
        public string healCrit;
        public string guild;
        public string playerName;
        public string playerDps;
        public string playerClass;
        public string playerTotalDamage;
        public string playerTotalDamagePercentage;
        public string playerAverageCritRate;
        public uint playerId;
        public List<KeyValuePair<string, string>> buffUptime = new List<KeyValuePair<string, string>>();
        public List<List<object>> buffDetail = new List<List<object>>();
        public List<SkillLog> skillLog = new List<SkillLog>();
    }
}
