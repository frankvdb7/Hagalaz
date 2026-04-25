using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using NSubstitute;
using Raido.Common.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameWorld.Tests.Network
{
    [TestClass]
    public class NpcRenderMasksWriterTests
    {
        [TestMethod]
        public void WriteRenderMasks_WithAnimationFlagButNullAnimation_DoesNotThrow()
        {
            // Arrange
            var writer = new NpcRenderMasksWriter(null!);
            var character = Substitute.For<ICharacter>();
            var npc = Substitute.For<INpc>();
            var renderInfo = Substitute.For<INpcRenderInformation>();
            var output = Substitute.For<IByteBufferWriter>();

            npc.RenderInformation.Returns(renderInfo);
            renderInfo.UpdateFlag.Returns(Hagalaz.Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Animation);
            renderInfo.CurrentAnimation.Returns((IAnimation?)null);

            // Act & Assert
            writer.WriteRenderMasks(character, npc, output, false);
        }
    }
}
