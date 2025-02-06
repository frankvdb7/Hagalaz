namespace Hagalaz.Game.Scripts.Model.Quests
{
    /// <summary>
    /// Contains the default quest script.
    /// </summary>
    public class DefaultQuestScript : QuestScriptBase
    {
        /// <summary>
        /// Contains suitable quest Id.
        /// </summary>
        private readonly int _suitableQuestID;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQuestScript" /> class.
        /// </summary>
        /// <param name="questId">The quest id.</param>
        public DefaultQuestScript(int questId) => _suitableQuestID = questId;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        /// Gets the quest id.
        /// </summary>
        /// <returns>The quest id this script is bound to.</returns>
        public override int GetQuestId() => _suitableQuestID;
    }
}