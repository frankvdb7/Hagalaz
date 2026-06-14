using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Network.Protocol._742;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Buffers;
using System;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Services.GameWorld.Tests.Network.Protocol._742
{
    [TestClass]
    public class CharacterRenderMasksWriterTests
    {
        private ItemStore _itemStore;
        private IHitSplatRenderTypeProvider _hitRenderMasks;
        private IBodyDataRepository _bodyDataRepository;
        private IClientMapDefinitionProvider _clientMapDefinitionProvider;
        private CharacterRenderMasksWriter _writer;

        [TestInitialize]
        public void Setup()
        {
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            var itemProviderMock = Substitute.For<ITypeProvider<IItemDefinition>>();
            var loggerMock = Substitute.For<Microsoft.Extensions.Logging.ILogger<ItemStore>>();
            _itemStore = new ItemStore(serviceProviderMock, itemProviderMock, loggerMock);

            _hitRenderMasks = Substitute.For<IHitSplatRenderTypeProvider>();
            _bodyDataRepository = Substitute.For<IBodyDataRepository>();
            _clientMapDefinitionProvider = Substitute.For<IClientMapDefinitionProvider>();
            _writer = new CharacterRenderMasksWriter(_itemStore, _hitRenderMasks, _bodyDataRepository, _clientMapDefinitionProvider);

            itemProviderMock.Get(Arg.Any<int>()).Returns((IItemDefinition)null);
        }

        [TestMethod]
        public void WriteAppearance_NpcTransformation_MissingItemDefinition_ShouldNotThrow()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            using var output = MemoryBufferWriter.Get();
            var appearance = Substitute.For<ICharacterAppearance>();
            var area = Substitute.For<IArea>();
            var areaScript = Substitute.For<IAreaScript>();

            character.Appearance.Returns(appearance);
            character.Area.Returns(area);
            area.Script.Returns(areaScript);
            areaScript.ShouldRenderBaseCombatLevel(character).Returns(true);

            appearance.NpcId.Returns(1); // triggers IsTransformedToNpc
            appearance.Size.Returns(1);

            _bodyDataRepository.BodySlotCount.Returns(1);
            _bodyDataRepository.IsDisabledSlot(Arg.Any<BodyPart>()).Returns(false);

            // 0x4000 + 1 = 16385, which triggers item definition lookup in transformation block
            appearance.GetDrawnBodyPart(Arg.Any<BodyPart>()).Returns(16385);

            // Act
            _writer.WriteAppearance(character, output, false);

            // Assert
            // If we reached here without exception, it passed.
        }

        [TestMethod]
        public void WriteRenderMasks_MissingAnimation_ShouldWritePlaceholders()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            using var output = MemoryBufferWriter.Get();
            var renderInfo = Substitute.For<ICharacterRenderInformation>();

            character.RenderInformation.Returns(renderInfo);
            renderInfo.UpdateFlag.Returns(Hagalaz.Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Animation);
            renderInfo.CurrentAnimation.Returns((IAnimation)null);

            // Act
            _writer.WriteRenderMasks(character, output, false);

            // Assert
            // Check that something was written (exact length depends on protocol, but placeholders should be there)
            Assert.IsTrue(output.Length > 0);
        }
    }
}
