using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
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
            var renderInfo = Substitute.For<INpcRenderInformation>();
            npc.RenderInformation.Returns(renderInfo);
            renderInfo.UpdateFlag.Returns(Hagalaz.Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Animation);
            renderInfo.CurrentAnimation.Returns((IAnimation?)null);
            var output = Substitute.For<IByteBufferWriter>();
            writer.WriteRenderMasks(null!, npc, output, false);
            output.Received().WriteByte(Arg.Any<byte>());
        }
    }
}
