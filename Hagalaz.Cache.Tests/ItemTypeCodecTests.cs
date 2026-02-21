using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ItemTypeCodecTests
    {
        private ItemType CreateFullyPopulatedItem(int id)
        {
            return new ItemType(id)
            {
                Name = "Test Item",
                InterfaceModelId = 12345,
                ModelZoom = 2500,
                ModelRotation1 = 180,
                ModelRotation2 = 90,
                ModelOffset1 = -5,
                ModelOffset2 = 5,
                StackableType = 1,
                Value = 100,
                MembersOnly = true,
                MaleWornModelId1 = 5000,
                MaleWornModelId2 = 5001,
                MaleWornModelId3 = 5002,
                FemaleWornModelId1 = 6000,
                FemaleWornModelId2 = 6001,
                FemaleWornModelId3 = 6002,
                GroundOptions = new string?[] { null, null, "take", "Test", null },
                InventoryOptions = new string?[] { "Wear", "Check", null, null, "drop" },
                OriginalModelColors = new int[] { 1, 2 },
                ModifiedModelColors = new int[] { 3, 4 },
                OriginalTextureColors = new int[] { 5, 6 },
                ModifiedTextureColors = new int[] { 7, 8 },
                Unnoted = true,
                NoteId = -1,
                NoteTemplateId = -1,
                StackIds = new int[] { 10, 20 },
                StackAmounts = new int[] { 100, 200 },
                ScaleX = 128,
                ScaleY = 128,
                ScaleZ = 128,
                Ambient = 10,
                Contrast = 20,
                TeamId = 5,
                LendId = -1,
                LendTemplateId = -1
            };
        }

        [Fact]
        public void TestEncodeDecode_AllProperties()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var originalItem = CreateFullyPopulatedItem(1);

            // Act
            var ms = codec.Encode(originalItem);
            ms.Position = 0;
            var decodedItem = (ItemType)codec.Decode(1, ms);

            // Assert
            Assert.Equal(originalItem.Id, decodedItem.Id);
            Assert.Equal(originalItem.Name, decodedItem.Name);
            Assert.Equal(originalItem.InterfaceModelId, decodedItem.InterfaceModelId);
            Assert.Equal(originalItem.ModelZoom, decodedItem.ModelZoom);
            Assert.Equal(originalItem.ModelRotation1, decodedItem.ModelRotation1);
            Assert.Equal(originalItem.ModelRotation2, decodedItem.ModelRotation2);
            Assert.Equal(originalItem.ModelOffset1, decodedItem.ModelOffset1);
            Assert.Equal(originalItem.ModelOffset2, decodedItem.ModelOffset2);
            Assert.Equal(originalItem.StackableType, decodedItem.StackableType);
            Assert.Equal(originalItem.Value, decodedItem.Value);
            Assert.Equal(originalItem.MembersOnly, decodedItem.MembersOnly);
            Assert.Equal(originalItem.MaleWornModelId1, decodedItem.MaleWornModelId1);
            Assert.Equal(originalItem.MaleWornModelId2, decodedItem.MaleWornModelId2);
            Assert.Equal(originalItem.MaleWornModelId3, decodedItem.MaleWornModelId3);
            Assert.Equal(originalItem.FemaleWornModelId1, decodedItem.FemaleWornModelId1);
            Assert.Equal(originalItem.FemaleWornModelId2, decodedItem.FemaleWornModelId2);
            Assert.Equal(originalItem.FemaleWornModelId3, decodedItem.FemaleWornModelId3);

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(originalItem.GroundOptions[i], decodedItem.GroundOptions[i]);
                Assert.Equal(originalItem.InventoryOptions[i], decodedItem.InventoryOptions[i]);
            }

            Assert.Equal(originalItem.OriginalModelColors, decodedItem.OriginalModelColors);
            Assert.Equal(originalItem.ModifiedModelColors, decodedItem.ModifiedModelColors);
            Assert.Equal(originalItem.OriginalTextureColors, decodedItem.OriginalTextureColors);
            Assert.Equal(originalItem.ModifiedTextureColors, decodedItem.ModifiedTextureColors);
            Assert.Equal(originalItem.Unnoted, decodedItem.Unnoted);
            Assert.Equal(originalItem.ScaleX, decodedItem.ScaleX);
            Assert.Equal(originalItem.ScaleY, decodedItem.ScaleY);
            Assert.Equal(originalItem.ScaleZ, decodedItem.ScaleZ);
            Assert.Equal(originalItem.Ambient, decodedItem.Ambient);
            Assert.Equal(originalItem.Contrast, decodedItem.Contrast);
            Assert.Equal(originalItem.TeamId, decodedItem.TeamId);
        }

        [Fact]
        public void Encode_WithNullOptions_ShouldNotThrow()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var item = new ItemType(1)
            {
                GroundOptions = new string?[] { null, "take", null, null, null },
                InventoryOptions = new string?[] { "Wear", null, null, null, "drop" }
            };

            // Act & Assert
            codec.Encode(item);
        }
    }
}
