// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Tera.Game;

namespace Tera.Data
{
    public class TeraData
    {
        public Region Region { get; private set; }
        public OpCodeNamer OpCodeNamer { get; private set; }
        public SkillDatabase SkillDatabase { get; private set; }
        public NpcDatabase NpcDatabase { get; private set; }

        internal TeraData(BasicTeraData basicData, string region)
        {
            SkillDatabase = new SkillDatabase(basicData.ResourceDirectory);
            NpcDatabase = new NpcDatabase(Path.Combine(basicData.ResourceDirectory, "npc-RU.txt"));
            OpCodeNamer = new OpCodeNamer(Path.Combine(basicData.ResourceDirectory, $"opcodes-{region}.txt"));
        }
    }
}
