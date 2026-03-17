using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic;
using Moq;


namespace Hagalaz.Cache.Tests.Logic
{
    [TestClass]
    public class ItemTypeLogicTests
    {
        private readonly Mock<IItemType> _mockItemType;

        public ItemTypeLogicTests()
        {
            _mockItemType = new Mock<IItemType>();
        }

        [TestMethod]
        public void HasSpecialBar_ShouldReturnTrue_WhenExtraDataContainsValue()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.HasSpecialBar, 1 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.HasSpecialBar();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void HasSpecialBar_ShouldReturnFalse_WhenExtraDataDoesNotContainValue()
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.HasSpecialBar();

            // Assert
            Assert.False(result);
        }

        [TestMethod]
        public void HasSpecialBar_ShouldReturnFalse_WhenExtraDataIsNull()
        {
            // Arrange
            _mockItemType.Setup(i => i.ExtraData).Returns((Dictionary<int, object>)null);

            // Act
            var result = _mockItemType.Object.HasSpecialBar();

            // Assert
            Assert.False(result);
        }

        [TestMethod]
        public void GetQuestId_ShouldReturnValue_WhenExtraDataContainsQuestId()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.QuestId, 123 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetQuestId();

            // Assert
            Assert.Equal(123, result);
        }

        [TestMethod]
        public void GetQuestId_ShouldReturnNegativeOne_WhenExtraDataDoesNotContainQuestId()
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetQuestId();

            // Assert
            Assert.Equal(-1, result);
        }

        [TestMethod]
        public void GetQuestId_ShouldReturnNegativeOne_WhenExtraDataIsNull()
        {
            // Arrange
            _mockItemType.Setup(i => i.ExtraData).Returns((Dictionary<int, object>)null);

            // Act
            var result = _mockItemType.Object.GetQuestId();

            // Assert
            Assert.Equal(-1, result);
        }

        [TestMethod]
        public void GetAttackSpeed_ShouldReturnValue_WhenExtraDataContainsAttackSpeed()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.AttackSpeed, 5 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetAttackSpeed();

            // Assert
            Assert.Equal(5, result);
        }

        [TestMethod]
        public void GetAttackSpeed_ShouldReturnDefault_WhenExtraDataDoesNotContainAttackSpeed()
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetAttackSpeed();

            // Assert
            Assert.Equal(4, result);
        }

        [TestMethod]
        public void GetAttackSpeed_ShouldReturnDefault_WhenExtraDataIsNull()
        {
            // Arrange
            _mockItemType.Setup(i => i.ExtraData).Returns((Dictionary<int, object>)null);

            // Act
            var result = _mockItemType.Object.GetAttackSpeed();

            // Assert
            Assert.Equal(4, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.StabAttack, 10)]
        [DataRow(ItemConstants.SlashAttack, 20)]
        [DataRow(ItemConstants.CrushAttack, 30)]
        [DataRow(ItemConstants.MagicAttack, 40)]
        [DataRow(ItemConstants.RangeAttack, 50)]
        public void GetAttackBonus_ShouldReturnValue_WhenExtraDataContainsKey(int key, int expectedValue)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { key, expectedValue } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.StabAttack => _mockItemType.Object.GetStabAttack(),
                ItemConstants.SlashAttack => _mockItemType.Object.GetSlashAttack(),
                ItemConstants.CrushAttack => _mockItemType.Object.GetCrushAttack(),
                ItemConstants.MagicAttack => _mockItemType.Object.GetMagicAttack(),
                ItemConstants.RangeAttack => _mockItemType.Object.GetRangeAttack(),
                _ => -1
            };

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.StabAttack)]
        [DataRow(ItemConstants.SlashAttack)]
        [DataRow(ItemConstants.CrushAttack)]
        [DataRow(ItemConstants.MagicAttack)]
        [DataRow(ItemConstants.RangeAttack)]
        public void GetAttackBonus_ShouldReturnZero_WhenExtraDataDoesNotContainKey(int key)
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.StabAttack => _mockItemType.Object.GetStabAttack(),
                ItemConstants.SlashAttack => _mockItemType.Object.GetSlashAttack(),
                ItemConstants.CrushAttack => _mockItemType.Object.GetCrushAttack(),
                ItemConstants.MagicAttack => _mockItemType.Object.GetMagicAttack(),
                ItemConstants.RangeAttack => _mockItemType.Object.GetRangeAttack(),
                _ => -1
            };

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.StabDefence, 10)]
        [DataRow(ItemConstants.SlashDefence, 20)]
        [DataRow(ItemConstants.CrushDefence, 30)]
        [DataRow(ItemConstants.MagicDefence, 40)]
        [DataRow(ItemConstants.RangeDefence, 50)]
        [DataRow(ItemConstants.SummoningDefence, 60)]
        public void GetDefenceBonus_ShouldReturnValue_WhenExtraDataContainsKey(int key, int expectedValue)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { key, expectedValue } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.StabDefence => _mockItemType.Object.GetStabDefence(),
                ItemConstants.SlashDefence => _mockItemType.Object.GetSlashDefence(),
                ItemConstants.CrushDefence => _mockItemType.Object.GetCrushDefence(),
                ItemConstants.MagicDefence => _mockItemType.Object.GetMagicDefence(),
                ItemConstants.RangeDefence => _mockItemType.Object.GetRangeDefence(),
                ItemConstants.SummoningDefence => _mockItemType.Object.GetSummoningDefence(),
                _ => -1
            };

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.StabDefence)]
        [DataRow(ItemConstants.SlashDefence)]
        [DataRow(ItemConstants.CrushDefence)]
        [DataRow(ItemConstants.MagicDefence)]
        [DataRow(ItemConstants.RangeDefence)]
        [DataRow(ItemConstants.SummoningDefence)]
        public void GetDefenceBonus_ShouldReturnZero_WhenExtraDataDoesNotContainKey(int key)
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.StabDefence => _mockItemType.Object.GetStabDefence(),
                ItemConstants.SlashDefence => _mockItemType.Object.GetSlashDefence(),
                ItemConstants.CrushDefence => _mockItemType.Object.GetCrushDefence(),
                ItemConstants.MagicDefence => _mockItemType.Object.GetMagicDefence(),
                ItemConstants.RangeDefence => _mockItemType.Object.GetRangeDefence(),
                ItemConstants.SummoningDefence => _mockItemType.Object.GetSummoningDefence(),
                _ => -1
            };

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.AbsorbMeleeBonus, 10)]
        [DataRow(ItemConstants.AbsorbMageBonus, 20)]
        [DataRow(ItemConstants.AbsorbRangeBonus, 30)]
        public void GetAbsorbBonus_ShouldReturnValue_WhenExtraDataContainsKey(int key, int expectedValue)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { key, expectedValue } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.AbsorbMeleeBonus => _mockItemType.Object.GetAbsorbMeleeBonus(),
                ItemConstants.AbsorbMageBonus => _mockItemType.Object.GetAbsorbMageBonus(),
                ItemConstants.AbsorbRangeBonus => _mockItemType.Object.GetAbsorbRangeBonus(),
                _ => -1
            };

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.AbsorbMeleeBonus)]
        [DataRow(ItemConstants.AbsorbMageBonus)]
        [DataRow(ItemConstants.AbsorbRangeBonus)]
        public void GetAbsorbBonus_ShouldReturnZero_WhenExtraDataDoesNotContainKey(int key)
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.AbsorbMeleeBonus => _mockItemType.Object.GetAbsorbMeleeBonus(),
                ItemConstants.AbsorbMageBonus => _mockItemType.Object.GetAbsorbMageBonus(),
                ItemConstants.AbsorbRangeBonus => _mockItemType.Object.GetAbsorbRangeBonus(),
                _ => -1
            };

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.StrengthBonus, 100, 10)]
        [DataRow(ItemConstants.RangedStrengthBonus, 200, 20)]
        public void GetStrengthBonus_ShouldReturnDividedValue_WhenExtraDataContainsKey(int key, int value, int expectedValue)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { key, value } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.StrengthBonus => _mockItemType.Object.GetStrengthBonus(),
                ItemConstants.RangedStrengthBonus => _mockItemType.Object.GetRangedStrengthBonus(),
                _ => -1
            };

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [TestMethod]
        [DataRow(ItemConstants.StrengthBonus)]
        [DataRow(ItemConstants.RangedStrengthBonus)]
        public void GetStrengthBonus_ShouldReturnZero_WhenExtraDataDoesNotContainKey(int key)
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = key switch
            {
                ItemConstants.StrengthBonus => _mockItemType.Object.GetStrengthBonus(),
                ItemConstants.RangedStrengthBonus => _mockItemType.Object.GetRangedStrengthBonus(),
                _ => -1
            };

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        public void GetMagicDamage_ShouldReturnValue_WhenExtraDataContainsKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.MagicDamage, 15 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetMagicDamage();

            // Assert
            Assert.Equal(15, result);
        }

        [TestMethod]
        public void GetMagicDamage_ShouldReturnZero_WhenExtraDataDoesNotContainKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetMagicDamage();

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        public void GetPrayerBonus_ShouldReturnValue_WhenExtraDataContainsKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.PrayerBonus, 5 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetPrayerBonus();

            // Assert
            Assert.Equal(5, result);
        }

        [TestMethod]
        public void GetPrayerBonus_ShouldReturnZero_WhenExtraDataDoesNotContainKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetPrayerBonus();

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        public void GetWeaponType_ShouldReturnValue_WhenExtraDataContainsKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.WeaponType, 7 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetWeaponType();

            // Assert
            Assert.Equal(7, result);
        }

        [TestMethod]
        public void GetWeaponType_ShouldReturnZero_WhenExtraDataDoesNotContainKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object>();
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetWeaponType();

            // Assert
            Assert.Equal(0, result);
        }

        [TestMethod]
        [DataRow(0, new int[] { 3, 3, 3, -1 })]
        [DataRow(1, new int[] { 3, 3, 3, -1 })]
        [DataRow(2, new int[] { 2, 2, 3, 2 })]
        [DataRow(13, new int[] { 4, 4, 4, -1 })]
        [DataRow(27, new int[] { 2, 1, 3, -1 })]
        [DataRow(28, new int[] { 3, 3, 3, -1 })] // Default case
        public void GetAttackBonusTypes_ShouldReturnCorrectArray_ForWeaponType(int weaponType, int[] expected)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.WeaponType, weaponType } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetAttackBonusTypes();

            // Assert
            Assert.Equal(expected, result);
        }

        [TestMethod]
        [DataRow(0, new int[] { 1, 2, 3, -1 })]
        [DataRow(1, new int[] { 1, 2, 3, -1 })]
        [DataRow(2, new int[] { 1, 2, 2, 3 })]
        [DataRow(13, new int[] { 5, 6, 7, -1 })]
        [DataRow(27, new int[] { 4, 4, 4, -1 })]
        [DataRow(28, new int[] { 1, 2, 3, -1 })] // Default case
        public void GetAttackStylesTypes_ShouldReturnCorrectArray_ForWeaponType(int weaponType, int[] expected)
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.WeaponType, weaponType } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetAttackStylesTypes();

            // Assert
            Assert.Equal(expected, result);
        }

        [TestMethod]
        public void GetEquipmentRequirements_ShouldReturnRequirements_WhenDataExists()
        {
            // Arrange
            var extraData = new Dictionary<int, object>
            {
                { 749, 0 }, { 750, 99 }, // Skill 0, level 99
                { 751, 1 }, { 752, 80 }  // Skill 1, level 80
            };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetEquipmentRequirements();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(99, result[0]);
            Assert.Equal(80, result[1]);
        }

        [TestMethod]
        public void GetEquipmentRequirements_ShouldReturnMaxedRequirement_WhenDataExists()
        {
            // Arrange
            var extraData = new Dictionary<int, object>
            {
                { ItemConstants.MaxedSkillRequirement, 5 } // Maxed skill 5
            };
            _mockItemType.Setup(i => i.Id).Returns(1); // Not 19709
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetEquipmentRequirements();

            // Assert
            Assert.Single(result);
            Assert.Equal(99, result[5]);
        }

        [TestMethod]
        public void GetEquipmentRequirements_ShouldReturn120_ForSpecificItemId()
        {
            // Arrange
            var extraData = new Dictionary<int, object>
            {
                { ItemConstants.MaxedSkillRequirement, 5 }
            };
            _mockItemType.Setup(i => i.Id).Returns(19709);
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetEquipmentRequirements();

            // Assert
            Assert.Single(result);
            Assert.Equal(120, result[5]);
        }

        [TestMethod]
        public void GetEquipmentRequirements_ShouldReturnEmpty_WhenNoDataExists()
        {
            // Arrange
            _mockItemType.Setup(i => i.ExtraData).Returns(new Dictionary<int, object>());

            // Act
            var result = _mockItemType.Object.GetEquipmentRequirements();

            // Assert
            Assert.Empty(result);
        }

        [TestMethod]
        public void IsNoted_ShouldReturnTrue_WhenNoted()
        {
            // Arrange
            _mockItemType.Setup(i => i.NoteId).Returns(1);
            _mockItemType.Setup(i => i.NoteTemplateId).Returns(1);

            // Act
            var result = _mockItemType.Object.IsNoted();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void IsNoted_ShouldReturnFalse_WhenNotNoted()
        {
            // Arrange
            _mockItemType.Setup(i => i.NoteId).Returns(-1);
            _mockItemType.Setup(i => i.NoteTemplateId).Returns(-1);

            // Act
            var result = _mockItemType.Object.IsNoted();

            // Assert
            Assert.False(result);
        }

        [TestMethod]
        public void IsStackable_ShouldReturnTrue_WhenStackableTypeIsOne()
        {
            // Arrange
            _mockItemType.Setup(i => i.StackableType).Returns(1);

            // Act
            var result = _mockItemType.Object.IsStackable();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void IsStackable_ShouldReturnTrue_WhenNoted()
        {
            // Arrange
            _mockItemType.Setup(i => i.StackableType).Returns(0);
            _mockItemType.Setup(i => i.NoteId).Returns(1);
            _mockItemType.Setup(i => i.NoteTemplateId).Returns(1);

            // Act
            var result = _mockItemType.Object.IsStackable();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void IsStackable_ShouldReturnFalse_WhenNotStackableOrNoted()
        {
            // Arrange
            _mockItemType.Setup(i => i.StackableType).Returns(0);
            _mockItemType.Setup(i => i.NoteId).Returns(-1);
            _mockItemType.Setup(i => i.NoteTemplateId).Returns(-1);

            // Act
            var result = _mockItemType.Object.IsStackable();

            // Assert
            Assert.False(result);
        }

        [TestMethod]
        public void HasWearModel_ShouldReturnTrue_WhenMaleModelExists()
        {
            // Arrange
            _mockItemType.Setup(i => i.MaleWornModelId1).Returns(1);

            // Act
            var result = _mockItemType.Object.HasWearModel();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void HasWearModel_ShouldReturnTrue_WhenFemaleModelExists()
        {
            // Arrange
            _mockItemType.Setup(i => i.MaleWornModelId1).Returns(-1);
            _mockItemType.Setup(i => i.FemaleWornModelId1).Returns(1);

            // Act
            var result = _mockItemType.Object.HasWearModel();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void HasWearModel_ShouldReturnFalse_WhenNoModelExists()
        {
            // Arrange
            _mockItemType.Setup(i => i.MaleWornModelId1).Returns(-1);
            _mockItemType.Setup(i => i.FemaleWornModelId1).Returns(-1);

            // Act
            var result = _mockItemType.Object.HasWearModel();

            // Assert
            Assert.False(result);
        }

        [TestMethod]
        public void GetDegradeType_ShouldReturnValue_WhenExtraDataContainsKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.DegradeType, (int)DegradeType.DestroyItem } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetDegradeType();

            // Assert
            Assert.Equal(DegradeType.DestroyItem, result);
        }

        [TestMethod]
        public void GetDegradeType_ShouldReturnDefault_WhenExtraDataDoesNotContainKey()
        {
            // Arrange
            _mockItemType.Setup(i => i.ExtraData).Returns(new Dictionary<int, object>());

            // Act
            var result = _mockItemType.Object.GetDegradeType();

            // Assert
            Assert.Equal(DegradeType.DropItem, result);
        }

        [TestMethod]
        public void GetRenderAnimationId_ShouldReturnValue_WhenExtraDataContainsKey()
        {
            // Arrange
            var extraData = new Dictionary<int, object> { { ItemConstants.RenderAnimationId, 123 } };
            _mockItemType.Setup(i => i.ExtraData).Returns(extraData);

            // Act
            var result = _mockItemType.Object.GetRenderAnimationId();

            // Assert
            Assert.Equal(123, result);
        }

        [TestMethod]
        public void GetRenderAnimationId_ShouldReturnNegativeOne_WhenExtraDataDoesNotContainKey()
        {
            // Arrange
            _mockItemType.Setup(i => i.ExtraData).Returns(new Dictionary<int, object>());

            // Act
            var result = _mockItemType.Object.GetRenderAnimationId();

            // Assert
            Assert.Equal(-1, result);
        }

        [TestMethod]
        public void HasDestroyOption_ShouldReturnTrue_WhenOptionExists()
        {
            // Arrange
            _mockItemType.Setup(i => i.InventoryOptions).Returns(new string[] { "Wield", "Destroy", "Drop" });

            // Act
            var result = _mockItemType.Object.HasDestroyOption();

            // Assert
            Assert.True(result);
        }

        [TestMethod]
        public void HasDestroyOption_ShouldReturnFalse_WhenOptionDoesNotExist()
        {
            // Arrange
            _mockItemType.Setup(i => i.InventoryOptions).Returns(new string[] { "Wield", "Drop" });

            // Act
            var result = _mockItemType.Object.HasDestroyOption();

            // Assert
            Assert.False(result);
        }

        [TestMethod]
        public void HasDestroyOption_ShouldReturnFalse_WhenOptionsAreNull()
        {
            // Arrange
            _mockItemType.Setup(i => i.InventoryOptions).Returns(new string[] { "Wield", null, "Drop" });

            // Act
            var result = _mockItemType.Object.HasDestroyOption();

            // Assert
            Assert.False(result);
        }
    }
}