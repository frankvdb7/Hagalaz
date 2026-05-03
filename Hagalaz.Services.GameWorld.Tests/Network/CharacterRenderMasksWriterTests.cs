using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Hagalaz.Services.GameWorld.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Buffers;

namespace Hagalaz.Services.GameWorld.Tests.Network
{
    [TestClass]
    public class CharacterRenderMasksWriterTests
    {
        [TestMethod]
        public void WriteRenderMasks_WhenAnimationIsNull_WritesZeros()
        {
            var writer = new CharacterRenderMasksWriter(null!, null!, null!, null!);
            var character = Substitute.For<ICharacter>();
            character.RenderInformation.UpdateFlag.Returns(UpdateFlags.Animation);
            character.RenderInformation.CurrentAnimation.Returns((IAnimation?)null);
            var output = Substitute.For<IByteBufferWriter>();

            writer.WriteRenderMasks(character, output, false);

            output.Received().WriteInt32BigEndianSmart(0);
        }
    }
}
