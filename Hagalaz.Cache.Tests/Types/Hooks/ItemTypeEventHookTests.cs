using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types.Hooks;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests.Types.Hooks
{
    public class ItemTypeEventHookTests
    {
        [Fact]
        public void AfterDecode_WithNoteTemplate_CallsMakeNote()
        {
            // Arrange
            var eventHook = new ItemTypeEventHook();
            var typeProviderMock = new Mock<ITypeProvider<IItemType>>();
            var itemTypeMock = new Mock<IItemType>();
            itemTypeMock.SetupGet(t => t.NoteTemplateId).Returns(1);
            itemTypeMock.SetupGet(t => t.NoteId).Returns(2);

            var noteMock = new Mock<IItemType>();
            var noteTemplateMock = new Mock<IItemType>();

            typeProviderMock.Setup(p => p.Get(2)).Returns(noteMock.Object);
            typeProviderMock.Setup(p => p.Get(1)).Returns(noteTemplateMock.Object);

            var types = new[] { itemTypeMock.Object };

            // Act
            eventHook.AfterDecode(typeProviderMock.Object, types);

            // Assert
            itemTypeMock.Verify(t => t.MakeNote(noteMock.Object, noteTemplateMock.Object), Times.Once);
        }

        [Fact]
        public void AfterDecode_WithLendTemplate_CallsMakeLend()
        {
            // Arrange
            var eventHook = new ItemTypeEventHook();
            var typeProviderMock = new Mock<ITypeProvider<IItemType>>();
            var itemTypeMock = new Mock<IItemType>();
            itemTypeMock.SetupGet(t => t.LendTemplateId).Returns(1);
            itemTypeMock.SetupGet(t => t.LendId).Returns(2);

            var lendMock = new Mock<IItemType>();
            var lendTemplateMock = new Mock<IItemType>();

            typeProviderMock.Setup(p => p.Get(2)).Returns(lendMock.Object);
            typeProviderMock.Setup(p => p.Get(1)).Returns(lendTemplateMock.Object);

            var types = new[] { itemTypeMock.Object };

            // Act
            eventHook.AfterDecode(typeProviderMock.Object, types);

            // Assert
            itemTypeMock.Verify(t => t.MakeLend(lendMock.Object, lendTemplateMock.Object), Times.Once);
        }

        [Fact]
        public void AfterDecode_WithBoughtTemplate_CallsMakeBought()
        {
            // Arrange
            var eventHook = new ItemTypeEventHook();
            var typeProviderMock = new Mock<ITypeProvider<IItemType>>();
            var itemTypeMock = new Mock<IItemType>();
            itemTypeMock.SetupGet(t => t.BoughtTemplateId).Returns(1);
            itemTypeMock.SetupGet(t => t.BoughtItemId).Returns(2);

            var boughtMock = new Mock<IItemType>();
            var boughtTemplateMock = new Mock<IItemType>();

            typeProviderMock.Setup(p => p.Get(2)).Returns(boughtMock.Object);
            typeProviderMock.Setup(p => p.Get(1)).Returns(boughtTemplateMock.Object);

            var types = new[] { itemTypeMock.Object };

            // Act
            eventHook.AfterDecode(typeProviderMock.Object, types);

            // Assert
            itemTypeMock.Verify(t => t.MakeBought(boughtMock.Object, boughtTemplateMock.Object), Times.Once);
        }
    }
}