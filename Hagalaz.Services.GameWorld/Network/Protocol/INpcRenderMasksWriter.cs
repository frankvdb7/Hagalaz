using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Raido.Common.Buffers;

namespace Hagalaz.Services.GameWorld.Network.Protocol
{
    public interface INpcRenderMasksWriter
    {
        void WriteRenderMasks(ICharacter character, INpc npc, IByteBufferWriter output, bool forceSynchronize);
    }
}
