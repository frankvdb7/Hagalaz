using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Skills.Fletching;

namespace Hagalaz.Game.Scripts.Skills.Firemaking
{
    /// <summary>
    ///     Contains logs item script.
    /// </summary>
    public class StandardLog : ItemScript
    {
        private readonly IFiremakingService _firemakingService;
        private readonly IFletchingSkillService _fletchingSkillService;
        private readonly IRsTaskService _taskService;
        private readonly IPathFinderProvider _pathFinderProvider;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly IGameObjectBuilder _gameObjectBuilder;

        public StandardLog(
            IFiremakingService firemakingService, IFletchingSkillService fletchingSkillService, IRsTaskService taskService,
            IPathFinderProvider pathFinderProvider, IGroundItemBuilder groundItemBuilder,
            IGameObjectBuilder gameObjectBuilder)
        {
            _firemakingService = firemakingService;
            _fletchingSkillService = fletchingSkillService;
            _taskService = taskService;
            _pathFinderProvider = pathFinderProvider;
            _groundItemBuilder = groundItemBuilder;
            _gameObjectBuilder = gameObjectBuilder;
        }

        /// <summary>
        ///     Uses the item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            if (used.Id == FiremakingConstants.Tinderbox)
            {
                return LightInventoryLog(character, usedWith);
            }

            if (usedWith.Id == FiremakingConstants.Tinderbox)
            {
                return LightInventoryLog(character, used);
            }

            return _fletchingSkillService.TryFletchWood(character, used, usedWith);
        }

        /// <summary>
        ///     Does ground tiem click actions when it is reached.
        /// </summary>
        public override void ItemClickedOnGroundPerform(IGroundItem item, GroundItemClickType clickType, ICharacter character)
        {
            character.Interrupt(this);
            if (clickType == GroundItemClickType.Option4Click)
            {
                character.QueueTask(() => LightGroundLog(character, item));
            }
            else
            {
                base.ItemClickedOnGroundPerform(item, clickType, character);
            }
        }

        public bool LightInventoryLog(ICharacter character, IItem logItem)
        {
            var slot = character.Inventory.GetInstanceSlot(logItem);
            if (slot == -1)
            {
                return false;
            }

            var definition = _firemakingService.FindByLogId(logItem.Id).Result;
            if (definition == null)
            {
                character.SendChatMessage("You can't light this log.");
                return false;
            }

            if (!logItem.ItemScript.DropItem(logItem, character))
            {
                return true;
            }

            var groundItemService = character.ServiceProvider.GetRequiredService<IGroundItemService>();
            var logs = groundItemService.FindAllGroundItems(character.Location).FirstOrDefault(i => i.ItemOnGround == logItem);
            if (logs == null)
            {
                return false;
            }

            character.QueueTask(() => LightGroundLog(character, logs));
            return true;
        }

        public async Task LightGroundLog(ICharacter character, IGroundItem logItem)
        {
            var log = await _firemakingService.FindByLogId(logItem.ItemOnGround.Id);
            if (log == null)
            {
                character.SendChatMessage("You can't light this log.");
                return;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Firemaking) < log.RequiredLevel)
            {
                character.SendChatMessage("You need firemaking level of " + log.RequiredLevel + " to light this log.");
                return;
            }

            var region = logItem.Region;
            if (region.FindStandardGameObject(logItem.Location.RegionLocalX, logItem.Location.RegionLocalY, logItem.Location.Z) != null)
            {
                character.SendChatMessage("You can't light a fire here.");
                return;
            }

            if (character.Inventory.GetById(FiremakingConstants.Tinderbox) == null)
            {
                character.SendChatMessage("You need a tinderbox if you intend on actually make a fire!");
                return;
            }

            void Callback()
            {
                region.Remove(logItem);
                var gameObj = _gameObjectBuilder.Create()
                    .WithId(log.FireObjectId)
                    .WithLocation(logItem.Location)
                    .Build();
                region.Add(gameObj);
                character.SendChatMessage("The fire catches and the logs begin to burn.");
                character.Statistics.AddExperience(StatisticsConstants.Firemaking, log.Experience);

                var path = _pathFinderProvider.Smart.FindAdjacent(character);
                if (path != null)
                {
                    character.Movement.AddToQueue(path);
                    character.FaceLocation(gameObj);
                }

                character.QueueTask(new RsTask(() => character.FaceLocation(gameObj), 1));
                _taskService.Schedule(new RsTask(() =>
                    {
                        region.Remove(gameObj);
                        var groundItem = _groundItemBuilder.Create()
                            .WithItem(itemBuilder =>  itemBuilder.Create().WithId(FiremakingConstants.Ashes))
                            .WithLocation(gameObj.Location)
                            .Build();
                        region.Add(groundItem);
                    },
                    log.Ticks));
            }

            character.QueueTask(new FiremakingTask(character, log, Callback));
            character.SendChatMessage("You attempt to light the logs.");
        }
    }
}