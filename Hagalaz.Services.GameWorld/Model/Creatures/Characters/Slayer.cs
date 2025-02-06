using System.Linq;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Configuration;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Game.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class Slayer : ISlayer, IHydratable<HydratedSlayerDto>, IDehydratable<HydratedSlayerDto>
    {
        /// <summary>
        /// The owner.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// The creature killed handler.
        /// </summary>
        private EventHappened? _creatureKilledHandler;

        /// <summary>
        /// The slayer task.
        /// </summary>
        private ISlayerTaskDefinition? _task;

        /// <summary>
        /// Gets the required count.
        /// </summary>
        /// <value>
        /// The required count.
        /// </value>
        public int CurrentKillCount { get; private set; }

        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        /// <value>
        /// The name of the task.
        /// </value>
        public string CurrentTaskName => _owner.HasSlayerTask() ? _task!.Name : string.Empty;

        /// <summary>
        /// Gets the slayer master identifier.
        /// </summary>
        /// <value>
        /// The slayer master identifier.
        /// </value>
        public int SlayerMasterId
        {
            get
            {
                if (_task != null)
                {
                    return _task.SlayerMasterId;
                }

                return -1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Slayer"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public Slayer(ICharacter owner) => _owner = owner;

        /// <summary>
        /// Called when [started].
        /// </summary>
        private void OnStarted()
        {
            if (_owner.HasSlayerTask())
            {
                _creatureKilledHandler = _owner.RegisterEventHandler(new EventHappened<CreatureKillEvent>((e) =>
                {
                    if (e.Victim is not INpc npc)
                    {
                        return false; // allow other events to catch a creature killed event.
                    }

                    if (_task == null || !_task.NpcIds.Contains(npc.Appearance.CompositeID))
                    {
                        return false; // allow other events to catch a creature killed event.
                    }

                    _owner.Statistics.AddExperience(StatisticsConstants.Slayer, npc.Definition.MaxLifePoints * 0.1);
                    CurrentKillCount--;
                    if (CurrentKillCount <= 0)
                    {
                        OnCompleted();
                    }

                    return false; // allow other events to catch a creature killed event.
                }));
            }
        }

        /// <summary>
        /// Called when [completed].
        /// </summary>
        private void OnCompleted()
        {
            if (_creatureKilledHandler != null)
            {
                _owner.UnregisterEventHandler<CreatureKillEvent>(_creatureKilledHandler);
                _creatureKilledHandler = null;
            }

            if (_task == null)
            {
                return;
            }

            if (_task.CoinCount > 0)
            {
                var ratesService = _owner.ServiceProvider.GetRequiredService<IRatesService>();
                var coinCountRate = ratesService.GetRate<ItemOptions>(i => i.CoinCountRate);
                // TODO - Calculate coins earned based on difficulty
                var coinsEarned = (int)(_task.CoinCount * coinCountRate);
                _owner.Inventory.TryAddItems(_owner, [(995, coinsEarned)], out _);
            }

            var slayerManager = _owner.ServiceProvider.GetRequiredService<ISlayerService>();
            var masterTable = slayerManager.FindSlayerMasterTableByNpcId(_task.SlayerMasterId).Result;
            if (masterTable == null)
            {
                return;
            }

            var pointsEarned = masterTable.BaseSlayerRewardPoints;
            // TODO - x5 points after 10 tasks, x15 points after 50 tasks
            _owner.Mediator.Publish(new ProfileIncrementIntAction(ProfileConstants.SlayerRewardPoints, pointsEarned));

            _owner.Widgets.OpenDialogue(_owner.ServiceProvider.GetRequiredService<ISlayerTaskCompletedDialogue>(), true);
            _task = null;
        }

        /// <summary>
        /// Assigns the new task.
        /// </summary>
        /// <param name="slayerMasterId">The slayer master identifier.</param>
        public void AssignNewTask(int slayerMasterId)
        {
            if (_creatureKilledHandler != null)
            {
                _owner.UnregisterEventHandler<CreatureKillEvent>(_creatureKilledHandler);
                _creatureKilledHandler = null;
            }

            var slayerManager = _owner.ServiceProvider.GetRequiredService<ISlayerService>();
            var slayerTaskGenerator = _owner.ServiceProvider.GetRequiredService<ISlayerTaskGenerator>();
            _owner.QueueTask(async () =>
            {
                var table = await slayerManager.FindSlayerMasterTableByNpcId(slayerMasterId);
                if (table == null)
                {
                    return;
                }

                var results = slayerTaskGenerator.GenerateTask(new SlayerTaskParams(table, _owner)).ToArray();

                var result = results.FirstOrDefault();
                if (result == null)
                {
                    return;
                }

                _task = result.Definition;
                CurrentKillCount = result.KillCount;
                OnStarted();
            });
        }

        public void Hydrate(HydratedSlayerDto hydration)
        {
            if (hydration.Task == null)
            {
                return;
            }

            var slayerManager = _owner.ServiceProvider.GetRequiredService<ISlayerService>();
            _task = slayerManager.FindSlayerTaskDefinition(hydration.Task.Id).Result;
            CurrentKillCount = hydration.Task.KillCount;
            OnStarted();
        }

        public HydratedSlayerDto Dehydrate()
        {
            if (_task != null)
            {
                return new HydratedSlayerDto
                {
                    Task = new HydratedSlayerDto.SlayerTaskDto
                    {
                        Id = _task.Id, KillCount = CurrentKillCount
                    }
                };
            }

            return new HydratedSlayerDto();
        }
    }
}