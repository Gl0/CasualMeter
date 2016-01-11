// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tera.Data;
using Tera.Game;
using Tera.Game.Messages;

namespace Tera.DamageMeter
{
    public class SkillResult
    {
        public DateTime Time { get; private set; }

        public int Amount { get; private set; }
        public Entity Source { get; private set; }
        public Entity Target { get; private set; }
        public bool IsCritical { get; private set; }
        public bool IsHeal { get; private set; }

        public int SkillId { get; private set; }
        public Skill Skill { get; private set; }
        public string SkillName => Skill?.Name ?? SkillId.ToString();
        public string SkillNameDetailed
            => $"{Skill?.Name ?? SkillId.ToString()} {(IsChained != null ? (bool) IsChained ? "[C]" : null : null)} {((Skill.Detail=="")? null : "("+Skill.Detail+")")}";
        public bool? IsChained => Skill.IsChained;
        public int Damage { get { return IsHeal ? 0 : Amount; } }
        public int Heal { get { return IsHeal ? Amount : 0; } }


        public Player SourcePlayer { get; private set; }
        public Player TargetPlayer { get; private set; }

        public SkillResult(EachSkillResultServerMessage message, EntityTracker entityRegistry, PlayerTracker playerTracker, SkillDatabase skillDatabase, NpcDatabase npcDatabase)
        {
            Time = message.Time;
            Amount = message.Amount;
            IsCritical = message.IsCritical;
            IsHeal = message.IsHeal;
            SkillId = message.SkillId;

            Debug.Assert(!message.IsUseless);

            Source = entityRegistry.GetOrPlaceholder(message.Source);
            Target = entityRegistry.GetOrPlaceholder(message.Target);
            var userNpc = UserEntity.ForEntity(Source);
            var npc = (NpcEntity)userNpc["npc"];
            var sourceUser = userNpc["user"] as UserEntity; // Attribute damage dealt by owned entities to the owner
            var targetUser = Target as UserEntity; // But don't attribute damage received by owned entities to the owner

            if (sourceUser != null)
            {
                Skill = skillDatabase.Get(sourceUser, message);
                if (Skill == null && npc != null)
                {
                    Skill = new UserSkill(message.SkillId, sourceUser.RaceGenderClass, npc.Info.Name);
                }
                SourcePlayer = playerTracker.Get(sourceUser.PlayerId);
                if (Skill == null)
                    Skill = new UserSkill(message.SkillId, sourceUser.RaceGenderClass, "Unknown");
            }
            if (targetUser != null)
            {
                TargetPlayer = playerTracker.Get(targetUser.PlayerId);
            }
        }

        public override string ToString()
        {
            return $"{SkillName}({SkillId}) [{Amount}]";
        }
    }
}
