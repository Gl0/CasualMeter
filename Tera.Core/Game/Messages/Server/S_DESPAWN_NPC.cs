namespace Tera.Game.Messages
{
    public class SDespawnNpc : ParsedMessage
    {
        internal SDespawnNpc(TeraMessageReader reader) : base(reader)
        {
            NPC = reader.ReadEntityId();
            reader.Skip(12);
            Dead = reader.ReadByte()== 5; // 1 = move out of view, 5 = death
        }

        public EntityId NPC { get; }
        public bool Dead { get; }
    }
}