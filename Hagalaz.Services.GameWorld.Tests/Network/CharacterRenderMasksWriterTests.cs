using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Cache.Abstractions.Types;
using NSubstitute;
using Raido.Common.Buffers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameWorld.Tests.Network
{
    [TestClass]
    public class CharacterRenderMasksWriterTests
    {
        [TestMethod]
        public void WriteRenderMasks_WithAnimationFlagButNullAnimation_DoesNotThrow()
        {
            // Arrange
            var writer = new CharacterRenderMasksWriter(null!, null!, null!, null!);
            var character = Substitute.For<ICharacter>();
            var renderInfo = Substitute.For<ICharacterRenderInformation>();
            var output = Substitute.For<IByteBufferWriter>();

            character.RenderInformation.Returns(renderInfo);
            renderInfo.UpdateFlag.Returns(UpdateFlags.Animation);
            renderInfo.CurrentAnimation.Returns((IAnimation?)null);

            // Act & Assert
            writer.WriteRenderMasks(character, output, false);
        }

        [TestMethod]
        public void WriteItemAppearance_WithItemPartButNullDefinition_DoesNotThrow()
        {
            // Arrange
            var itemProvider = Substitute.For<ITypeProvider<IItemDefinition>>();
            itemProvider.Get(Arg.Any<int>()).Returns((IItemDefinition)null!);

            var itemStore = new ItemStore(null!, itemProvider, null!);
            var writer = new CharacterRenderMasksWriter(itemStore, null!, null!, null!);
            var character = Substitute.For<ICharacter>();
            var appearance = Substitute.For<ICharacterAppearance>();
            var itemPart = Substitute.For<IItemPart>();
            var output = Substitute.For<IByteBufferWriter>();

            character.Appearance.Returns(appearance);
            appearance.GetDrawnItemPart(Arg.Any<BodyPart>()).Returns(itemPart);
            itemPart.ItemId.Returns(1);
            itemPart.Flags.Returns(ItemUpdateFlags.Model);

            // Act & Assert
            writer.WriteItemAppearance(character, output);
        }
    }
}
