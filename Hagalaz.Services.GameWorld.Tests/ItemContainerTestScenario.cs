using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class Scenario : IDisposable
{
        private readonly ServiceProvider _services;
        private readonly Dictionary<int, IItemDefinition> _itemDefinitions = [];

        public ICharacter Owner { get; } = Substitute.For<ICharacter>();
        public IItemBuilder Builder { get; }
        public IItemDefinition DefaultItemDefinition { get; }
        public IEquipmentDefinition DefaultEquipmentDefinition { get; }

        public Scenario()
        {
            Owner.Statistics.Returns(Substitute.For<ICharacterStatistics>());
            DefaultItemDefinition = CreateDefinition(stackable: true);
            DefaultEquipmentDefinition = Substitute.For<IEquipmentDefinition>();
            var itemScript = Substitute.For<IItemScript>();
            itemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(callInfo =>
            {
                var current = callInfo.ArgAt<IItem>(0);
                var incoming = callInfo.ArgAt<IItem>(1);
                return current.Id == incoming.Id && current.ExtraData.SequenceEqual(incoming.ExtraData) && (callInfo.ArgAt<bool>(2) || current.ItemDefinition.Stackable || current.ItemDefinition.Noted);
            });
            var equipmentScript = Substitute.For<IEquipmentScript>();
            var itemService = Substitute.For<IItemService>();
            itemService.FindItemDefinitionById(Arg.Any<int>()).Returns(callInfo =>
            {
                var id = callInfo.Arg<int>();
                return _itemDefinitions.TryGetValue(id, out var definition) ? definition : DefaultItemDefinition;
            });
            var equipmentService = Substitute.For<IEquipmentService>();
            equipmentService.FindEquipmentDefinitionById(Arg.Any<int>()).Returns(DefaultEquipmentDefinition);
            var itemProvider = Substitute.For<IItemScriptProvider>();
            itemProvider.FindItemScriptById(Arg.Any<int>()).Returns(itemScript);
            var equipmentProvider = Substitute.For<IEquipmentScriptProvider>();
            equipmentProvider.FindEquipmentScriptById(Arg.Any<int>()).Returns(equipmentScript);
            _services = new ServiceCollection()
                .AddSingleton(itemService)
                .AddSingleton(equipmentService)
                .BuildServiceProvider();
            Builder = new ItemBuilder(_services, itemProvider, equipmentProvider);
        }

        public void DefineItem(int id, bool stackable, bool noted = false, int noteId = -1)
        {
            _itemDefinitions[id] = CreateDefinition(stackable, noted, noteId);
        }

        public void DefineItem(int id, IItemDefinition definition) => _itemDefinitions[id] = definition;

        private static IItemDefinition CreateDefinition(bool stackable, bool noted = false, int noteId = -1)
        {
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(stackable);
            definition.Noted.Returns(noted);
            definition.NoteId.Returns(noteId);
            return definition;
        }

        public void Dispose() => _services.Dispose();
}
