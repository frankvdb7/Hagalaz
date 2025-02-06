using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class Quests
    {
        /// <summary>
        /// The owner.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Contains the quests of the character.
        /// </summary>
        private readonly Dictionary<short, IQuest> _quests = new Dictionary<short, IQuest>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Quests"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public Quests(ICharacter owner) => _owner = owner;

        /// <summary>
        /// Gets the quest status.
        /// </summary>
        /// <param name="questID">The quest Id.</param>
        /// <returns></returns>
        public QuestPrimaryStatus GetQuestStatus(short questID)
        {
            if (HasActiveQuest(questID))
                return _quests[questID].PrimaryStatus;
            return QuestPrimaryStatus.NotStarted;
        }

        /// <summary>
        /// Gets the name of an active quest by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Returns null if no active quest by name is found.</returns>
        public IQuest? GetActiveQuestByName(string name)
        {
            foreach (var pair in _quests)
                if (pair.Value.Name.Equals(name))
                    return pair.Value;
            return null;
        }

        /// <summary>
        /// Gets the active quest by id.
        /// </summary>
        /// <param name="questID">The quest Id.</param>
        /// <returns></returns>
        public IQuest? GetActiveQuestById(short questID)
        {
            if (HasActiveQuest(questID))
                return _quests[questID];
            return null;
        }

        /// <summary>
        /// Determines whether [has active quest] [the specified quest Id].
        /// </summary>
        /// <param name="questID">The quest Id.</param>
        /// <returns>
        ///   <c>true</c> if [has active quest] [the specified quest Id]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasActiveQuest(short questID) => questID > 0 && _quests.ContainsKey(questID);
    }
}