using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Appereance class for npc.
    /// </summary>
    public class NpcAppearance : INpcAppearance
    {
        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly Npc _owner;

        /// <summary>
        /// Contains npc composite Id.
        /// </summary>
        public int CompositeID { get; private set; }

        /// <summary>
        /// Contains size of npc.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Contains boolean if npc is visible.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Construct's new appearance class.
        /// </summary>
        /// <param name="owner"></param>
        public NpcAppearance(Npc owner)
        {
            _owner = owner;
            CompositeID = _owner.Definition.Id;
            Size = _owner.Definition.Size;
            Visible = true;
        }

        /// <summary>
        /// Transforms this npc to other npc.
        /// </summary>
        /// <param name="compositeID">The composite identifier.</param>
        public void Transform(int compositeID)
        {
            if (CompositeID != compositeID)
            {
                CompositeID = compositeID;
                Size = _owner.ServiceProvider.GetRequiredService<INpcService>().FindNpcDefinitionById(compositeID).Size;
                _owner.RenderInformation.ScheduleFlagUpdate(UpdateFlags.Transform);
            }
        }
    }
}