using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Raido.Common.Buffers;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Services.GameWorld.Tests.Network.Protocol
{
    [TestClass]
    public class CharacterRenderMasksWriterTests
    {
        private ItemStore _itemStore;
        private IHitSplatRenderTypeProvider _hitRenderMasksMock;
        private IBodyDataRepository _bodyDataRepositoryMock;
        private IClientMapDefinitionProvider _clientMapDefinitionProviderMock;
        private CharacterRenderMasksWriter _writer;
        private ITypeProvider<IItemDefinition> _itemProviderMock;

        [TestInitialize]
        public void Setup()
        {
            _itemProviderMock = Substitute.For<ITypeProvider<IItemDefinition>>();
            _itemStore = new ItemStore(Substitute.For<IServiceProvider>(), _itemProviderMock, Substitute.For<Microsoft.Extensions.Logging.ILogger<ItemStore>>());

            _hitRenderMasksMock = Substitute.For<IHitSplatRenderTypeProvider>();
            _bodyDataRepositoryMock = Substitute.For<IBodyDataRepository>();
            _clientMapDefinitionProviderMock = Substitute.For<IClientMapDefinitionProvider>();
            _writer = new CharacterRenderMasksWriter(_itemStore, _hitRenderMasksMock, _bodyDataRepositoryMock, _clientMapDefinitionProviderMock);
        }

        [TestMethod]
        public void WriteItemAppearance_MissingItemDefinition_WritesZeroByteAndDoesNotThrow()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            characterMock.Appearance.Returns(appearanceMock);

            var itemPartMock = Substitute.For<IItemPart>();
            itemPartMock.ItemId.Returns(123);
            itemPartMock.Flags.Returns(ItemUpdateFlags.Model);
            itemPartMock.MaleModels.Returns(new[] { 1 });
            itemPartMock.FemaleModels.Returns(new[] { 1 });

            _bodyDataRepositoryMock.BodySlotCount.Returns(1);
            _bodyDataRepositoryMock.IsDisabledSlot(Arg.Any<BodyPart>()).Returns(false);

            appearanceMock.GetDrawnItemPart(Arg.Any<BodyPart>()).Returns(itemPartMock);

            // Mock ITypeProvider to return null for the item definition
            _itemProviderMock.Get(123).Returns((IItemDefinition)null!);

            var outputBuffer = Substitute.For<IByteBufferWriter>();

            // Act
            _writer.WriteItemAppearance(characterMock, outputBuffer);

            // Assert
            outputBuffer.Received(1).WriteByte(0);
        }
    }
}
