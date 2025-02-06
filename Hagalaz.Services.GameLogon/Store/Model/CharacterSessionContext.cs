namespace Hagalaz.Services.GameLogon.Store.Model
{
    public class CharacterSessionContext
    {
        public uint Id { get; }
        public int WorldId { get; }

        public CharacterSessionContext(uint id, int worldId)
        {
            Id = id;
            WorldId = worldId;
        }
    }
}