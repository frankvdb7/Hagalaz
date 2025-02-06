using System.Text;
using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    public class GameObjectDefinition : ObjectType, IGameObjectDefinition
    {
        public string Examine { get; set; }
        public int LootTableId { get; set; }

        public GameObjectDefinition(int id)
            : base(id) => Examine = "It's an object.";

        public override string ToString() =>
            new StringBuilder("GameObjectDefinition")
                .Append('[')
                .AppendFormat("id={0}, name={1}, size_x={2}, size_y={3}, solid={4}, gateway={5}, clip_type={6}, varp_id={7}, actions={8}",
                    Id, Name, SizeX, SizeY, Solid, Gateway, ClipType, VarpBitFileId, Actions)
                .Append(']').ToString();
    }
}