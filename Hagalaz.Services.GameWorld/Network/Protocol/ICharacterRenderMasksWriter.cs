using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Raido.Common.Buffers;

namespace Hagalaz.Services.GameWorld.Network.Protocol
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterRenderMasksWriter
    {
        void WriteRenderMasks(ICharacter character, IByteBufferWriter output, bool forceSynchronize);
    }
}