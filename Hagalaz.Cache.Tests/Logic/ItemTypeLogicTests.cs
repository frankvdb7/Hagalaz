using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace Hagalaz.Cache.Tests.Logic
{
    public class ItemTypeLogicTests
    {
        private readonly Mock<IItemType> _itemTypeMock;

        public ItemTypeLogicTests()
        {
            _itemTypeMock = new Mock<IItemType>();
        }

        [Fact]
        public void HasSpecialBar_WhenExtraDataIsNull_ShouldReturnFalse()
        {
            // Arrange
            _itemTypeMock.Setup(x => x.ExtraData).Returns((IReadOnlyDictionary<int, object>)null);

            // Act
            var result = _itemTypeMock.Object.HasSpecialBar();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasSpecialBar_WhenKeyIsMissing_ShouldReturnFalse()
        {
            // Arrange
            _itemTypeMock.Setup(x => x.ExtraData).Returns(new Dictionary<int, object>());

            // Act
            var result = _itemTypeMock.Object.HasSpecialBar();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasSpecialBar_WhenValueIsOne_ShouldReturnTrue()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.HasSpecialBar, 1 } };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.HasSpecialBar();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetQuestId_WhenKeyIsPresent_ShouldReturnQuestId()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.QuestId, 123 } };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetQuestId();

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public void GetAttackSpeed_WhenKeyIsMissing_ShouldReturnDefault()
        {
            // Arrange
            _itemTypeMock.Setup(x => x.ExtraData).Returns(new Dictionary<int, object>());

            // Act
            var result = _itemTypeMock.Object.GetAttackSpeed();

            // Assert
            Assert.Equal(4, result);
        }

        [Fact]
        public void GetStabAttack_WhenKeyIsPresent_ShouldReturnBonus()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.StabAttack, 50 } };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetStabAttack();

            // Assert
            Assert.Equal(50, result);
        }

        [Fact]
        public void GetStrengthBonus_WhenKeyIsPresent_ShouldReturnBonusDividedByTen()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.StrengthBonus, 150 } };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetStrengthBonus();

            // Assert
            Assert.Equal(15, result);
        }

        [Theory]
        [InlineData(1, new int[] { 3, 3, 3, -1 })]
        [InlineData(13, new int[] { 4, 4, 4, -1 })]
        [InlineData(27, new int[] { 2, 1, 3, -1 })]
        [InlineData(99, new int[] { 3, 3, 3, -1 })] // Default case
        public void GetAttackBonusTypes_ShouldReturnCorrectTypesForWeaponType(int weaponType, int[] expectedBonuses)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.WeaponType, weaponType } };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetAttackBonusTypes();

            // Assert
            Assert.Equal(expectedBonuses, result);
        }

        [Theory]
        [InlineData(1, new int[] { 1, 2, 3, -1 })]
        [InlineData(13, new int[] { 5, 6, 7, -1 })]
        [InlineData(27, new int[] { 4, 4, 4, -1 })]
        [InlineData(99, new int[] { 1, 2, 3, -1 })] // Default case
        public void GetAttackStylesTypes_ShouldReturnCorrectTypesForWeaponType(int weaponType, int[] expectedStyles)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.WeaponType, weaponType } };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetAttackStylesTypes();

            // Assert
            Assert.Equal(expectedStyles, result);
        }

        [Fact]
        public void GetEquipmentRequirements_WhenDataIsPresent_ShouldReturnRequirements()
        {
            // Arrange
            var extraData = new Dictionary<int, object>
            {
                { 749, 0 }, // Skill: Attack
                { 750, 60 }, // Level: 60
                { 751, 2 }, // Skill: Strength
                { 752, 75 }  // Level: 75
            };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetEquipmentRequirements();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(60, result[0]);
            Assert.Equal(75, result[2]);
        }

        [Fact]
        public void GetEquipmentRequirements_WithMaxedSkill_ShouldReturnCorrectLevel()
        {
            // Arrange
            var extraData = new Dictionary<int, object>
            {
                { ItemConstants.MaxedSkillRequirement, 1 } // Skill: Defence
            };
            _itemTypeMock.Setup(x => x.ExtraData).Returns(extraData);

            // Act
            var result = _itemTypeMock.Object.GetEquipmentRequirements();

            // Assert
            Assert.Single(result);
            Assert.Equal(99, result[1]);
        }

        [Fact]
        public void IsNoted_WhenNoted_ReturnsTrue()
        {
            // Arrange
            _itemTypeMock.Setup(i => i.NoteId).Returns(1);
            _itemTypeMock.Setup(i => i.NoteTemplateId).Returns(1);

            // Act
            var result = _itemTypeMock.Object.IsNoted();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsNoted_WhenNotNoted_ReturnsFalse()
        {
            // Arrange
            _itemTypeMock.Setup(i => i.NoteId).Returns(-1);
            _itemTypeMock.Setup(i => i.NoteTemplateId).Returns(-1);

            // Act
            var result = _itemTypeMock.Object.IsNoted();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasDestroyOption_WhenPresent_ReturnsTrue()
        {
            // Arrange
            _itemTypeMock.Setup(i => i.InventoryOptions).Returns(new[] { "Wield", "Examine", "Destroy" });

            // Act
            var result = _itemTypeMock.Object.HasDestroyOption();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasDestroyOption_WhenNotPresent_ReturnsFalse()
        {
            // Arrange
            _itemTypeMock.Setup(i => i.InventoryOptions).Returns(new[] { "Wield", "Examine" });

            // Act
            var result = _itemTypeMock.Object.HasDestroyOption();

            // Assert
            Assert.False(result);
        }
    }
}