using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Skills
{
    /// <summary>
    ///     Contains skills levelup interface.
    /// </summary>
    public class LevelupInterface : WidgetScript
    {
        /// <summary>
        ///     Contains total level milestones.
        /// </summary>
        private static readonly int[] _totalLevelMilestones = [1, 25, 50, 75, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1800, 1900, 2000, 2100, 2200, 2300, 2400, 2500, 2600, 2700, 2800, 2900, 3000
        ];

        /// <summary>
        ///     Contains combat level milestones.
        /// </summary>
        private static readonly int[] _combatLevelMilestones = [3, 5, 10, 15, 25, 50, 75, 90];

        /// <summary>
        ///     Contains levelup ids for skill ids.
        /// </summary>
        private static readonly int[] _levelupIds = [1, 5, 2, 6, 3, 7, 4, 16, 18, 19, 15, 17, 11, 14, 13, 9, 8, 10, 20, 21, 12, 22, 23, 24, 25];

        /// <summary>
        ///     Contains ICONFIG ids for skill ids. TODO
        /// </summary>
        private static readonly int[] _iconfigIDs = [1469, 1470, 1471, 1472, 1474, 1473, 1475, 1476, 1477, 1478, 1479, 1480, 1481, 1482, 1483, 1484, 1485, 1486, 1487, 1488, 1489, 1490, 1491, 1492, 1492, 1493
        ];

        public LevelupInterface(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
        }

        /// <summary>
        ///     Set's up this interface.
        /// </summary>
        /// <param name="skillID">Id of the skill which was leveled up.</param>
        public void Setup(byte skillID)
        {
            /*
                skillType LSH 3
                reached total level milestone LSH 2 (boolean) ( | 4 )
                reached combat level milestone LSH 1 (boolean) ( | 2 )
                unknown boolean LSH 0 (boolean) ( | 1 )
                combat level milestone LSH 23 (combat level / 5 (only for 5 , 10 , 15 etc )) 
                total level milestone LSH 27 (25,50,75,100,200,300 & so on )
             */
            var levelupID = _levelupIds[skillID];
            var combatMs = GetCombatLevelMilestoneID();
            var totalMs = GetTotalLevelMilestoneID();

            uint config = 1;

            if (combatMs != -1)
            {
                config |= (uint)combatMs << 23;
                config |= 0x2;
            }

            if (totalMs != -1)
            {
                config |= (uint)totalMs << 27;
                config |= 0x4;
            }

            config |= (uint)levelupID << 3;
            Owner.Configurations.SendGlobalCs2Int((short)_iconfigIDs[skillID], StatisticsHelpers.LevelForExperience(skillID, Owner.Statistics.GetPreviousSkillExperience(skillID)));
            Owner.Configurations.SendStandardConfiguration(1230, (int)config);
        }


        /// <summary>
        ///     Get's reached total level milestone Id or -1 if no milestone
        ///     was reached.
        /// </summary>
        /// <returns></returns>
        public int GetTotalLevelMilestoneID()
        {
            var totalLevel = Owner.Statistics.TotalLevel;
            for (var i = 0; i < _totalLevelMilestones.Length; i++)
            {
                if (_totalLevelMilestones[i] == totalLevel)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Get's reached combat level milestone Id or -1 if no milestone
        ///     was reached.
        /// </summary>
        /// <returns></returns>
        public int GetCombatLevelMilestoneID()
        {
            var combatLevel = Owner.Statistics.FullCombatLevel;
            for (var i = 0; i < _combatLevelMilestones.Length; i++)
            {
                if (_combatLevelMilestones[i] == combatLevel)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}