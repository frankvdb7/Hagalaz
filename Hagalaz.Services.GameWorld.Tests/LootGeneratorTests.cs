using AwesomeAssertions;
using Hagalaz.Services.GameWorld.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Random;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class LootGeneratorTests
    {
        private LootGenerator _generator;
        private IRandomProvider _randomProviderMock;

        [TestInitialize]
        public void Setup()
        {
            _randomProviderMock = Substitute.For<IRandomProvider>();
            _generator = new LootGenerator(_randomProviderMock);
        }

        [TestMethod]
        public void GenerateLoot_WithEmptyTable_ShouldReturnNoItems()
        {
            // Arrange
            var table = new TestLootTable();
            var lootParams = new LootParams(table) { MaxCount = 1 };

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GenerateLoot_WithDisabledTable_ShouldReturnNoItems()
        {
            // Arrange
            var table = new TestLootTable { Enabled = false };
            var lootParams = new LootParams(table) { MaxCount = 1 };

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GenerateLoot_WithAlwaysDrop_ShouldReturnItem()
        {
            // Arrange
            var item = new TestLootItem { Id = 1, Always = true };
            var table = new TestLootTable { Entries = new List<ILootObject> { item } };
            var lootParams = new LootParams(table) { MaxCount = 1 };
            _randomProviderMock.Next(Arg.Any<int>(), Arg.Any<int>()).Returns(1);

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().HaveCount(1);
            result.First().Item.Should().Be(item);
        }

        [TestMethod]
        public void GenerateLoot_WithMaxCount_ShouldReturnCorrectNumberOfItems()
        {
            // Arrange
            var item1 = new TestLootItem { Id = 1, Probability = 1 };
            var item2 = new TestLootItem { Id = 2, Probability = 1 };
            var item3 = new TestLootItem { Id = 3, Probability = 1 };
            var table = new TestLootTable { Entries = new List<ILootObject> { item1, item2, item3 } };
            var lootParams = new LootParams(table) { MaxCount = 2 };
            _randomProviderMock.Next(Arg.Any<int>()).Returns(0);

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().HaveCount(2);
        }

        [TestMethod]
        public void GenerateLoot_WithNestedTable_ShouldReturnItemsFromBothTables()
        {
            // Arrange
            var nestedItem = new TestLootItem { Id = 1, Always = true };
            var nestedTable = new TestLootTable { Entries = new List<ILootObject> { nestedItem }, MaxResultCount = 1, Always = true };
            var mainItem = new TestLootItem { Id = 2, Always = true };
            var mainTable = new TestLootTable { Entries = new List<ILootObject> { mainItem, nestedTable }, MaxResultCount = 2 };
            var lootParams = new LootParams(mainTable) { MaxCount = 2 };
            _randomProviderMock.Next(Arg.Any<int>(), Arg.Any<int>()).Returns(1);

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(r => r.Item.Id == mainItem.Id);
            result.Should().Contain(r => r.Item.Id == nestedItem.Id);
        }

        [TestMethod]
        public void GenerateLoot_WithRandomizeResultCount_ShouldReturnRandomNumberOfItems()
        {
            // Arrange
            var item1 = new TestLootItem { Id = 1, Probability = 1 };
            var item2 = new TestLootItem { Id = 2, Probability = 1 };
            var item3 = new TestLootItem { Id = 3, Probability = 1 };
            var table = new TestLootTable { Entries = new List<ILootObject> { item1, item2, item3 }, RandomizeResultCount = true };
            var lootParams = new LootParams(table) { MaxCount = 3 };
            _randomProviderMock.Next(0, 4).Returns(2);
            _randomProviderMock.Next(Arg.Any<int>()).Returns(0);

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().HaveCount(2);
        }

        [TestMethod]
        public void GenerateLoot_WithProbabilityModifier_ShouldAffectDropRate()
        {
            // Arrange
            var item1 = new TestLootItem { Id = 1, Probability = 1 };
            var modifier = Substitute.For<IRandomObjectModifier>();
            modifier.Apply(Arg.Do<RandomObjectContext>(c => c.ModifiedProbability *= 2));
            var table = new TestLootTable { Entries = new List<ILootObject> { item1 }, Modifiers = new List<IRandomObjectModifier> { modifier } };
            var lootParams = new LootParams(table) { MaxCount = 1 };
            _randomProviderMock.Next(Arg.Any<int>()).Returns(0);

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            modifier.Received().Apply(Arg.Any<RandomObjectContext>());
        }

        [TestMethod]
        public void GenerateLoot_WithCountModifier_ShouldAffectItemCount()
        {
            // Arrange
            var item1 = new TestLootItem { Id = 1, Always = true };
            var modifier = Substitute.For<IRandomObjectModifier>();
            modifier.Apply(Arg.Do<RandomObjectContext>(c =>
            {
                if (c is LootContext lootContext)
                {
                    lootContext.ModifiedMinimumCount = 5;
                    lootContext.ModifiedMaximumCount = 5;
                }
            }));
            var table = new TestLootTable { Entries = new List<ILootObject> { item1 }, Modifiers = new List<IRandomObjectModifier> { modifier } };
            var lootParams = new LootParams(table) { MaxCount = 1 };
            _randomProviderMock.Next(5, 6).Returns(5);

            // Act
            var result = _generator.GenerateLoot<TestLootItem>(lootParams);

            // Assert
            result.Should().HaveCount(1);
            result.First().Count.Should().Be(5);
        }

        private class TestLootItem : ILootItem
        {
            public int Id { get; set; }
            public int MinimumCount { get; set; } = 1;
            public int MaximumCount { get; set; } = 1;
            public bool Enabled { get; set; } = true;
            public double Probability { get; set; } = 1;
            public bool Always { get; set; } = false;
        }

        private class TestLootTable : ILootTable
        {
            public bool Enabled { get; set; } = true;
            public int MaxCount { get; set; }
            public bool RandomizeResultCount { get; set; }
            public IReadOnlyList<ILootObject> Entries { get; set; } = new List<ILootObject>();
            public IReadOnlyList<IRandomObjectModifier> Modifiers { get; set; } = new List<IRandomObjectModifier>();
            public int MaxResultCount { get; set; }
            public bool Always { get; set; }
            public double Probability { get; set; }
        }

    }
}
