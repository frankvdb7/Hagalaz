using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Hagalaz.Services.GameWorld.Store;
using Raido.Common.Buffers;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model;
using System;
using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Tests.Network.Protocol._742
{
    [TestClass]
    public class CharacterRenderMasksWriterTests
    {
        private ItemStore _itemStore;
        private ITypeProvider<IItemDefinition> _itemProviderMock;
        private IHitSplatRenderTypeProvider _hitRenderMasksMock;
        private IBodyDataRepository _bodyDataRepositoryMock;
        private IClientMapDefinitionProvider _clientMapDefinitionProviderMock;
        private CharacterRenderMasksWriter _writer;

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
        public void WriteItemAppearance_MissingDefinition_WritesZeroFlagsAndDoesNotThrow()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var appearanceMock = Substitute.For<ICharacterAppearance>();
            characterMock.Appearance.Returns(appearanceMock);

            _bodyDataRepositoryMock.BodySlotCount.Returns(1);
            _bodyDataRepositoryMock.IsDisabledSlot(Arg.Any<BodyPart>()).Returns(false);

            var itemAppearanceMock = Substitute.For<IItemPart>();
            itemAppearanceMock.ItemId.Returns(999);
            itemAppearanceMock.Flags.Returns(ItemUpdateFlags.Model);
            itemAppearanceMock.MaleModels.Returns(new[] { 1 });
            itemAppearanceMock.FemaleModels.Returns(new[] { 1 });

            appearanceMock.GetDrawnItemPart((BodyPart)0).Returns(itemAppearanceMock);
            _itemProviderMock.Get(999).Returns((IItemDefinition)null!);

            var outputBuffer = Substitute.For<IByteBufferWriter>();
            var writtenBytes = new List<byte>();
            outputBuffer.WriteByte(Arg.Do<byte>(b => writtenBytes.Add(b)));

            // Act
            _writer.WriteItemAppearance(characterMock, outputBuffer);

            // Assert
            Assert.AreEqual(1, writtenBytes.Count);
            Assert.AreEqual(0, writtenBytes[0]);
        }
    }
}
