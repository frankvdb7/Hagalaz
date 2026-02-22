using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;
using System.Collections.Generic;

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
                MaleHeadModel = 7000,
                MaleHeadModel2 = 7001,
                FemaleHeadModel = 8000,
                FemaleHeadModel2 = 8001,
                GroundOptions = new string?[] { null, null, "take", "Test", null },
                InventoryOptions = new string?[] { "Wear", "Check", null, null, "drop" },
                OriginalModelColors = new int[] { 1, 2 },
                ModifiedModelColors = new int[] { 3, 4 },
                OriginalTextureColors = new int[] { 5, 6 },
                ModifiedTextureColors = new int[] { 7, 8 },
                Unnoted = true,
                NoteId = -1,
                NoteTemplateId = -1,
                StackIds = new int[] { 10, 20, 0, 0, 0, 0, 0, 0, 0, 0 },
                StackAmounts = new int[] { 100, 200, 0, 0, 0, 0, 0, 0, 0, 0 },
                ScaleX = 150,
                ScaleY = 160,
                ScaleZ = 170,
                Ambient = 10,
                Contrast = 20,
                TeamId = 5,
                LendId = 121,
                LendTemplateId = -1,
                Zan2D = 128,
                UnknownInt6 = 42,
                ExtraData = new Dictionary<int, object> { { 1, "test" }, { 2, 123 } }
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
            AssertEqualAllProperties(originalItem, decodedItem);
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

        [Fact]
        public void Encode_Decode_RoundTrip_NotedItem_ShouldRestoreCorrectData()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var item = CreateFullyPopulatedItem(100);
            item.NoteId = 101;
            item.NoteTemplateId = 799;

            // Act
            var ms = codec.Encode(item);
            ms.Position = 0;
            var decoded = (ItemType)codec.Decode(100, ms);

            // Adjust expected item because Noted items skip Name, StackableType, and MembersOnly in Encode
            var expected = CreateFullyPopulatedItem(100);
            expected.NoteId = 101;
            expected.NoteTemplateId = 799;
            expected.Name = "null"; // Default in constructor
            expected.StackableType = 0; // Default in constructor
            expected.MembersOnly = false; // Default in constructor

            // Assert
            AssertEqualAllProperties(expected, decoded);
        }

        [Fact]
        public void Encode_Decode_RoundTrip_LentItem_ShouldRestoreCorrectData()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var item = CreateFullyPopulatedItem(200);
            item.LendId = 201;
            item.LendTemplateId = 899;

            // Act
            var ms = codec.Encode(item);
            ms.Position = 0;
            var decoded = (ItemType)codec.Decode(200, ms);

            // Adjust expected item because Lent items skip Value in Encode
            var expected = CreateFullyPopulatedItem(200);
            expected.LendId = 201;
            expected.LendTemplateId = 899;
            expected.Value = 1; // Default in constructor

            // Assert
            AssertEqualAllProperties(expected, decoded);
        }

        private void AssertEqualAllProperties(ItemType expected, ItemType actual)
        {
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.InterfaceModelId, actual.InterfaceModelId);
            Assert.Equal(expected.ModelZoom, actual.ModelZoom);
            Assert.Equal(expected.ModelRotation1, actual.ModelRotation1);
            Assert.Equal(expected.ModelRotation2, actual.ModelRotation2);
            Assert.Equal(expected.ModelOffset1, actual.ModelOffset1);
            Assert.Equal(expected.ModelOffset2, actual.ModelOffset2);
            Assert.Equal(expected.StackableType, actual.StackableType);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.MembersOnly, actual.MembersOnly);
            Assert.Equal(expected.MaleWornModelId1, actual.MaleWornModelId1);
            Assert.Equal(expected.MaleWornModelId2, actual.MaleWornModelId2);
            Assert.Equal(expected.MaleWornModelId3, actual.MaleWornModelId3);
            Assert.Equal(expected.FemaleWornModelId1, actual.FemaleWornModelId1);
            Assert.Equal(expected.FemaleWornModelId2, actual.FemaleWornModelId2);
            Assert.Equal(expected.FemaleWornModelId3, actual.FemaleWornModelId3);
            Assert.Equal(expected.MaleHeadModel, actual.MaleHeadModel);
            Assert.Equal(expected.FemaleHeadModel, actual.FemaleHeadModel);
            Assert.Equal(expected.MaleHeadModel2, actual.MaleHeadModel2);
            Assert.Equal(expected.FemaleHeadModel2, actual.FemaleHeadModel2);
            Assert.Equal(expected.Zan2D, actual.Zan2D);
            Assert.Equal(expected.UnknownInt6, actual.UnknownInt6);

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(expected.GroundOptions[i], actual.GroundOptions[i]);
                Assert.Equal(expected.InventoryOptions[i], actual.InventoryOptions[i]);
            }

            Assert.Equal(expected.OriginalModelColors, actual.OriginalModelColors);
            Assert.Equal(expected.ModifiedModelColors, actual.ModifiedModelColors);
            Assert.Equal(expected.OriginalTextureColors, actual.OriginalTextureColors);
            Assert.Equal(expected.ModifiedTextureColors, actual.ModifiedTextureColors);
            Assert.Equal(expected.Unnoted, actual.Unnoted);
            Assert.Equal(expected.ScaleX, actual.ScaleX);
            Assert.Equal(expected.ScaleY, actual.ScaleY);
            Assert.Equal(expected.ScaleZ, actual.ScaleZ);
            Assert.Equal(expected.Ambient, actual.Ambient);
            Assert.Equal(expected.Contrast, actual.Contrast);
            Assert.Equal(expected.TeamId, actual.TeamId);
            Assert.Equal(expected.LendId, actual.LendId);
            Assert.Equal(expected.LendTemplateId, actual.LendTemplateId);
            Assert.Equal(expected.StackIds, actual.StackIds);
            Assert.Equal(expected.StackAmounts, actual.StackAmounts);
            Assert.Equal(expected.ExtraData, actual.ExtraData);
        }
    }
}
