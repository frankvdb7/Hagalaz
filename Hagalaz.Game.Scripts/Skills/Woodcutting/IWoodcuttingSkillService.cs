using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Woodcutting
{
    public sealed record WoodcuttingPreparation(
        LogDto Logs,
        TreeDto Tree,
        IReadOnlyList<HatchetDto> Hatchets,
        ILootTable? LootTable,
        int CharacterCount);

    public interface IWoodcuttingSkillService
    {
        Task<WoodcuttingPreparation?> PrepareCuttingAsync(ICharacter character, IGameObject tree, CancellationToken cancellationToken);
        void StartCutting(ICharacter character, IGameObject tree, WoodcuttingPreparation preparation);
    }
}
