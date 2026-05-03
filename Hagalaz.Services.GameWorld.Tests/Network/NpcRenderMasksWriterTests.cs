using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Hagalaz.Services.GameWorld.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Buffers;

namespace Hagalaz.Services.GameWorld.Tests.Network
{
    [TestClass]
    public class NpcRenderMasksWriterTests
    {
        [TestMethod]
        public void WriteRenderMasks_WhenAnimationIsNull_WritesZeros()
        {
            var writer = new NpcRenderMasksWriter(null!);
            var npc = Substitute.For<INpc>();
            var character = Substitute.For<ICharacter>();
            npc.RenderInformation.UpdateFlag.Returns(Hagalaz.Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Animation);
            npc.RenderInformation.CurrentAnimation.Returns((IAnimation?)null);
            var output = Substitute.For<IByteBufferWriter>();

            writer.WriteRenderMasks(character, npc, output, false);

            output.Received().WriteInt32BigEndianSmart(0);
        }
    }
}
