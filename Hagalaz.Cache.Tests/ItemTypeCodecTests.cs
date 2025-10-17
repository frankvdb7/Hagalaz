using System.Collections.Generic;
using System.IO;
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
                Value = 1000,
                MembersOnly = true,
                MaleWornModelId1 = 101,
                MaleWornModelId2 = 102,
                FemaleWornModelId1 = 201,
                FemaleWornModelId2 = 202,
                GroundOptions = new string?[] { "Take", "Examine", "Eat", null, null },
                InventoryOptions = new string?[] { "Use", "Wield", "Check", "Destroy", "Drop" },
                OriginalModelColors = new[] { 60, 70 },
                ModifiedModelColors = new[] { 80, 90 },
                OriginalTextureColors = new[] { 10, 20 },
                ModifiedTextureColors = new[] { 30, 40 },
                UnknownArray1 = new sbyte[] { 1, 2, 3 },
                QuestIDs = new[] { 10, 20, 30 },
                Unnoted = true,
                MaleWornModelId3 = 103,
                FemaleWornModelId3 = 203,
                MultiStackSize = 500,
                MaleHeadModel = 301,
                FemaleHeadModel = 401,
                MaleHeadModel2 = 302,
                FemaleHeadModel2 = 402,
                Zan2D = 123,
                UnknownInt6 = 255,
                NoteId = -1,
                NoteTemplateId = -1,
                StackIds = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                StackAmounts = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 },
                ScaleX = 256,
                ScaleY = 256,
                ScaleZ = 256,
                Ambient = 20,
                Contrast = 100,
                TeamId = 5,
                LendId = -1,
                LendTemplateId = -1,
                MaleWearXOffset = 4,
                MaleWearYOffset = 8,
                MaleWearZOffset = 12,
                FemaleWearXOffset = 16,
                FemaleWearYOffset = 20,
                FemaleWearZOffset = 24,
                UnknownInt18 = 1,
                UnknownInt19 = 2,
                UnknownInt20 = 3,
                UnknownInt21 = 4,
                UnknownInt22 = 5,
                UnknownInt23 = 6,
                UnknownInt24 = 7,
                UnknownInt25 = 8,
                PickSizeShift = 2,
                BoughtItemId = -1,
                BoughtTemplateId = -1,
                EquipSlot = 1,
                EquipType = 2,
                SomeEquipInt = 3,
                ExtraData = new Dictionary<int, object>
                {
                    { 1, "string value" },
                    { 2, 12345 },
                    { 255, "another string" }
                }
            };
        }

        [Fact]
        public void Encode_Decode_RoundTrip_StandardItem_ShouldRestoreAllData()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var originalItem = CreateFullyPopulatedItem(1);

            // Act
            var encodedStream = codec.Encode(originalItem);
            encodedStream.Position = 0;
            var decodedItem = (ItemType)codec.Decode(originalItem.Id, encodedStream);

            // Assert
            AssertEqualAllProperties(originalItem, decodedItem);
        }

        [Fact]
        public void Encode_Decode_RoundTrip_NotedItem_ShouldRestoreCorrectData()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var originalItem = CreateFullyPopulatedItem(2);
            originalItem.NoteId = 1;
            originalItem.NoteTemplateId = 799;

            // Act
            var encodedStream = codec.Encode(originalItem);
            encodedStream.Position = 0;
            var decodedItem = (ItemType)codec.Decode(originalItem.Id, encodedStream);

            // Assert
            // These properties are NOT encoded for noted items, so they will have their default values.
            Assert.Equal("null", decodedItem.Name);
            Assert.Equal(0, decodedItem.StackableType);
            Assert.False(decodedItem.MembersOnly);

            // All other properties should be equal. We can copy the original and adjust the expected defaults.
            var expectedItem = CreateFullyPopulatedItem(2);
            expectedItem.NoteId = 1;
            expectedItem.NoteTemplateId = 799;
            expectedItem.Name = "null";
            expectedItem.StackableType = 0;
            expectedItem.MembersOnly = false;

            AssertEqualAllProperties(expectedItem, decodedItem);
        }

        [Fact]
        public void Encode_Decode_RoundTrip_LentItem_ShouldRestoreCorrectData()
        {
            // Arrange
            var codec = new ItemTypeCodec();
            var originalItem = CreateFullyPopulatedItem(3);
            originalItem.LendId = 1;
            originalItem.LendTemplateId = 800;

            // Act
            var encodedStream = codec.Encode(originalItem);
            encodedStream.Position = 0;
            var decodedItem = (ItemType)codec.Decode(originalItem.Id, encodedStream);

            // Assert
            // The Value property is NOT encoded for lent items, so it will have its default value.
            Assert.Equal(1, decodedItem.Value);

            // All other properties should be equal.
            var expectedItem = CreateFullyPopulatedItem(3);
            expectedItem.LendId = 1;
            expectedItem.LendTemplateId = 800;
            expectedItem.Value = 1;

            AssertEqualAllProperties(expectedItem, decodedItem);
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
            Assert.Equal(expected.FemaleWornModelId1, actual.FemaleWornModelId1);
            Assert.Equal(expected.FemaleWornModelId2, actual.FemaleWornModelId2);
            Assert.Equal(expected.GroundOptions, actual.GroundOptions);
            Assert.Equal(expected.InventoryOptions, actual.InventoryOptions);
            Assert.Equal(expected.OriginalModelColors, actual.OriginalModelColors);
            Assert.Equal(expected.ModifiedModelColors, actual.ModifiedModelColors);
            Assert.Equal(expected.OriginalTextureColors, actual.OriginalTextureColors);
            Assert.Equal(expected.ModifiedTextureColors, actual.ModifiedTextureColors);
            Assert.Equal(expected.UnknownArray1, actual.UnknownArray1);
            Assert.Equal(expected.QuestIDs, actual.QuestIDs);
            Assert.Equal(expected.Unnoted, actual.Unnoted);
            Assert.Equal(expected.MaleWornModelId3, actual.MaleWornModelId3);
            Assert.Equal(expected.FemaleWornModelId3, actual.FemaleWornModelId3);
            Assert.Equal(expected.MultiStackSize, actual.MultiStackSize);
            Assert.Equal(expected.MaleHeadModel, actual.MaleHeadModel);
            Assert.Equal(expected.FemaleHeadModel, actual.FemaleHeadModel);
            Assert.Equal(expected.MaleHeadModel2, actual.MaleHeadModel2);
            Assert.Equal(expected.FemaleHeadModel2, actual.FemaleHeadModel2);
            Assert.Equal(expected.Zan2D, actual.Zan2D);
            Assert.Equal(expected.UnknownInt6, actual.UnknownInt6);
            Assert.Equal(expected.NoteId, actual.NoteId);
            Assert.Equal(expected.NoteTemplateId, actual.NoteTemplateId);
            Assert.Equal(expected.StackIds, actual.StackIds);
            Assert.Equal(expected.StackAmounts, actual.StackAmounts);
            Assert.Equal(expected.ScaleX, actual.ScaleX);
            Assert.Equal(expected.ScaleY, actual.ScaleY);
            Assert.Equal(expected.ScaleZ, actual.ScaleZ);
            Assert.Equal(expected.Ambient, actual.Ambient);
            Assert.Equal(expected.Contrast, actual.Contrast);
            Assert.Equal(expected.TeamId, actual.TeamId);
            Assert.Equal(expected.LendId, actual.LendId);
            Assert.Equal(expected.LendTemplateId, actual.LendTemplateId);
            Assert.Equal(expected.MaleWearXOffset, actual.MaleWearXOffset);
            Assert.Equal(expected.MaleWearYOffset, actual.MaleWearYOffset);
            Assert.Equal(expected.MaleWearZOffset, actual.MaleWearZOffset);
            Assert.Equal(expected.FemaleWearXOffset, actual.FemaleWearXOffset);
            Assert.Equal(expected.FemaleWearYOffset, actual.FemaleWearYOffset);
            Assert.Equal(expected.FemaleWearZOffset, actual.FemaleWearZOffset);
            Assert.Equal(expected.UnknownInt18, actual.UnknownInt18);
            Assert.Equal(expected.UnknownInt19, actual.UnknownInt19);
            Assert.Equal(expected.UnknownInt20, actual.UnknownInt20);
            Assert.Equal(expected.UnknownInt21, actual.UnknownInt21);
            Assert.Equal(expected.UnknownInt22, actual.UnknownInt22);
            Assert.Equal(expected.UnknownInt23, actual.UnknownInt23);
            Assert.Equal(expected.UnknownInt24, actual.UnknownInt24);
            Assert.Equal(expected.UnknownInt25, actual.UnknownInt25);
            Assert.Equal(expected.PickSizeShift, actual.PickSizeShift);
            Assert.Equal(expected.BoughtItemId, actual.BoughtItemId);
            Assert.Equal(expected.BoughtTemplateId, actual.BoughtTemplateId);
            Assert.Equal(expected.EquipSlot, actual.EquipSlot);
            Assert.Equal(expected.EquipType, actual.EquipType);
            Assert.Equal(expected.SomeEquipInt, actual.SomeEquipInt);
            Assert.Equal(expected.ExtraData, actual.ExtraData);
        }
    }
}