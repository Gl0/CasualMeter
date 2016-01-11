// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Tera.Game.Messages;

namespace Tera.Game
{
    // A player character, including your own
    public class UserEntity : Entity
    {
        public string Name { get; set; }
        public string GuildName { get; set; }
        public RaceGenderClass RaceGenderClass { get; set; }
        public uint PlayerId { get; }

        public UserEntity(EntityId id)
            : base(id)
        {
        }

        internal UserEntity(SpawnUserServerMessage message)
            : this(message.Id)
        {
            Name = message.Name;
            GuildName = message.GuildName;
            RaceGenderClass = message.RaceGenderClass;
            PlayerId = message.PlayerId;
        }

        internal UserEntity(LoginServerMessage message)
            : this(message.Id)
        {
            Name = message.Name;
            GuildName = message.GuildName;
            RaceGenderClass = message.RaceGenderClass;
            PlayerId = message.PlayerId;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", Name, GuildName);
        }

        public static Dictionary<string, Entity> ForEntity(Entity entity)
        {
            Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
            var ownedEntity = entity as IHasOwner;
            while (ownedEntity != null && ownedEntity.Owner != null)
            {
                if (entity.GetType() == typeof(NpcEntity))
                    entities.Add("npc", (NpcEntity)entity);
                entity = ownedEntity.Owner;
                ownedEntity = entity as IHasOwner;
            }
            entities.Add("user", (Entity)entity);
            if (!entities.ContainsKey("npc"))
                entities.Add("npc", null);

            return entities;
        }

        public override bool Equals(object obj)
        {
            var other = obj as UserEntity;
            return PlayerId.Equals(other?.PlayerId);
        }

        public override int GetHashCode()
        {
            return PlayerId.GetHashCode();
        }
    }
}
