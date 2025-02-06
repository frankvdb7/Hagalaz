namespace Hagalaz.Game.Scripts.Skills.Thieving
{
    /// <summary>
    /// </summary>
    public struct StealDefinition
    {
        /// <summary>
        ///     The game object IDs.
        /// </summary>
        public int[] GameObjectIDs;

        /// <summary>
        ///     The required level.
        /// </summary>
        public byte RequiredLevel;

        /// <summary>
        ///     The experience.
        /// </summary>
        public double Experience;

        /// <summary>
        ///     The respawn ticks.
        /// </summary>
        public int RespawnTicks;

        /// <summary>
        ///     The NPC owner Id.
        /// </summary>
        public short NpcOwnerID;

        /// <summary>
        ///     The replace object Id.
        /// </summary>
        public int EmptyGameObjectID;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StealDefinition" /> struct.
        /// </summary>
        /// <param name="gameObjectIDs">The game object I ds.</param>
        /// <param name="requiredLevel">The required level.</param>
        /// <param name="experience">The experience.</param>
        /// <param name="respawnTicks">The respawn ticks.</param>
        /// <param name="npcOwnerId">The NPC owner id.</param>
        /// <param name="emptyGameObjectID">The empty game object Id.</param>
        public StealDefinition(int[] gameObjectIDs, byte requiredLevel, double experience, int respawnTicks, short npcOwnerId, int emptyGameObjectID = 34381)
        {
            GameObjectIDs = gameObjectIDs;
            RequiredLevel = requiredLevel;
            Experience = experience;
            RespawnTicks = respawnTicks;
            NpcOwnerID = npcOwnerId;
            EmptyGameObjectID = emptyGameObjectID;
        }
    }
}