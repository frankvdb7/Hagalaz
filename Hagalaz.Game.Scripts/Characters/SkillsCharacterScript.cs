using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Characters
{
    public class SkillsCharacterScript : CharacterScriptBase, IDefaultCharacterScript
    {
        private readonly IAudioBuilder _soundBuilder;
        private readonly IGraphicBuilder _graphicBuilder;
        private readonly IGameMessageService _gameMessageService;

        /// <summary>
        /// The skill sound effects.
        /// </summary>
        private static readonly short[] _skillMusicEffects =
        [
            30, 38, 66, 48, 58, 56, 52, 34, 70, 44, 42, 40, 36, 64, 54, 46, 28, 68, 61, 10, 60, 50, 32, 301, 417
        ];

        /// <summary>
        /// The skill cs2 int values
        /// </summary>
        private static readonly byte[] _skillCs2IntValues =
        [
            1, 5, 2, 6, 3, 7, 4, 16, 18, 19, 15, 17, 11, 14, 13, 9, 8, 10, 20, 21, 12, 22, 23, 24, 25
        ];

        public SkillsCharacterScript(
            ICharacterContextAccessor contextAccessor, IAudioBuilder soundBuilder, IGraphicBuilder graphicBuilder, IGameMessageService gameMessageService)
            : base(contextAccessor)
        {
            _soundBuilder = soundBuilder;
            _graphicBuilder = graphicBuilder;
            _gameMessageService = gameMessageService;
        }

        protected override void Initialize() { }

        public override void OnRegistered()
        {
            Character.RegisterEventHandler<SkillLevelUpEvent>(@event =>
            {
                HandleSkillLevelUp(@event.SkillID, @event.CurrentSkillLevel);
                return false;
            });
        }

        private void HandleSkillLevelUp(int skillID, int currentLevel)
        {
            Character.Statistics.FlashSkill(skillID);
            Character.SendChatMessage("You've just advanced a " + StatisticsConstants.SkillNames[skillID] + " level! You have reached level " +
                                      Character.Statistics.LevelForExperience(skillID) + ".");
            Character.QueueGraphic(_graphicBuilder.Create().WithId(199).WithDelay(100).Build());
            var musicEffect = _soundBuilder.Create().AsMusicEffect().WithId(_skillMusicEffects[skillID]).Build();
            Character.Session.SendMessage(musicEffect.ToMessage());

            if (Character.Widgets.GetOpenWidget(1216) == null)
            {
                var defaultScript = Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                Character.Widgets.OpenWidget(1216, (short)(Character.GameClient.IsScreenFixed ? 50 : 68), 1, defaultScript, false);
            }

            Character.Configurations.SendGlobalCs2Int(1756, _skillCs2IntValues[skillID]);

            if (currentLevel >= 99 || currentLevel >= 120)
            {
                Character.QueueGraphic(_graphicBuilder.Create().WithId(1765).Build());

                // TODO
                //var database = ServiceLocator.Current.GetInstance<ISqlDatabaseManager>();
                //await database.ExecuteAsync(new ActivityLogQuery(Character.MasterId, "Level-up", "I have achieved level " + currentLevel + " in " + StatisticsConstants.SkillNames[skillID] + "."));
                var lvl99Count = 0;
                for (var i = 0; i < StatisticsConstants.SkillsCount; i++)
                    if (Character.Statistics.LevelForExperience(i) >= 99)
                        lvl99Count++;
                if (currentLevel == 99 && lvl99Count == StatisticsConstants.SkillsCount)
                {
                    _gameMessageService.MessageAsync(Character.DisplayName + " has achieved at least level 99 in all skills!",
                            GameMessageType.WorldSpecific,
                            Character.DisplayName)
                        .Wait();
                    //await database.ExecuteAsync(new ActivityLogQuery(Character.MasterId, "Level-max", "I have achieved at least level 99 in all skills!"));
                }
                else
                {
                    _gameMessageService.MessageAsync(
                            Character.DisplayName + " has achieved level " + currentLevel + " in " + StatisticsConstants.SkillNames[skillID] + "!",
                            GameMessageType.WorldSpecific,
                            Character.DisplayName)
                        .Wait();
                }
            }
        }
    }
}