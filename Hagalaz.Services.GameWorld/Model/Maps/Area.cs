using System.Drawing;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using QuadTrees.QTreeRect;

namespace Hagalaz.Services.GameWorld.Model.Maps
{
    /// <summary>
    /// Represents single area.
    /// </summary>
    public class Area : IRectQuadStorable, IArea
    {
        /// <summary>
        /// Contains area Id.
        /// </summary>
        /// <value>The Id.</value>
        public int Id { get; }

        /// <summary>
        /// Contains area name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Wheter this area is multicombat zone.
        /// </summary>
        /// <value><c>true</c> if [multi combat]; otherwise, <c>false</c>.</value>
        public bool MultiCombat { get; }

        /// <summary>
        /// Wheter this area is pvp.
        /// </summary>
        /// <value><c>true</c> if [PvP]; otherwise, <c>false</c>.</value>
        public bool IsPvP { get; }

        /// <summary>
        /// Wether a dwarf mulit cannon is allowed in this area.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [cannon allowed]; otherwise, <c>false</c>.
        /// </value>
        public bool CannonAllowed { get; }

        /// <summary>
        /// Wether a familiar is allowed in this area.
        /// </summary>
        public bool FamiliarAllowed { get; }

        /// <summary>
        /// Contains area script.
        /// </summary>
        /// <value>The script.</value>
        public IAreaScript Script { get; }

        /// <summary>
        /// Contains the rect.
        /// </summary>
        public Rectangle Rect { get; }

        /// <summary>
        /// Construct's new area.
        /// </summary>
        /// <param name="id">The Id.</param>
        /// <param name="name">The name.</param>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="pvp">if set to <c>true</c> [PVP].</param>
        /// <param name="multiCombat">if set to <c>true</c> [multi combat].</param>
        /// <param name="cannonAllowed">if set to <c>true</c> [cannon allowed].</param>
        /// <param name="familiarAllowed">if set to <c>true</c> [familiar allowed].</param>
        /// <param name="script">The script.</param>
        public Area(int id, string name, ILocation minimum, ILocation maximum, bool pvp, bool multiCombat, bool cannonAllowed, bool familiarAllowed, IAreaScript script)
        {
            Id = id;
            Name = name;
            IsPvP = pvp;
            MultiCombat = multiCombat;
            CannonAllowed = cannonAllowed;
            FamiliarAllowed = familiarAllowed;
            Rect = new Rectangle(minimum.X, minimum.Y, maximum.X - minimum.X, maximum.Y - minimum.Y);

            Script = script;
            Script.Initialize(this);
        }

        /// <summary>
        /// Happens when character enters this area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public void OnCreatureEnterArea(ICreature creature)
        {
            Script.OnCreatureEnterArea(creature);
            Script.RenderEnterArea(creature);
        }

        /// <summary>
        /// Happens when character exits this area.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public void OnCreatureExitArea(ICreature creature)
        {
            Script.RenderExitArea(creature);
            Script.OnCreatureExitArea(creature);
        }
    }
}