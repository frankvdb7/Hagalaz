using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Contains NPC rendering methods.
    /// </summary>
    public partial class Npc : Creature
    {
        /// <summary>
        /// Get's if this npc should be rendered for specific character.
        /// </summary>
        /// <param name="viewer">The character.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool ShouldBeRenderedFor(ICharacter viewer) => Appearance.Visible;

        /// <summary>
        /// Get's if this npc should be rendered for specific npc.
        /// </summary>
        /// <param name="viewer">The NPC.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool ShouldBeRenderedFor(INpc viewer) => Appearance.Visible;

        /// <summary>
        /// Happens when this npc movement type is changed.
        /// </summary>
        /// <param name="newtype">New movement type.</param>
        public override void MovementTypeChanged(MovementType newtype)
        {
        }

        /// <summary>
        /// Inform's client that specific movement type was enabled for this tick only.
        /// </summary>
        /// <param name="type">The type.</param>
        public override void TemporaryMovementTypeEnabled(MovementType type)
        {
        }

        /// <summary>
        /// Get's called when npc faces specific character.
        /// If character is null then npc is not facing anything anymore.
        /// </summary>
        /// <param name="creature">The creature.</param>
        protected override void CreatureFaced(ICreature? creature) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.FaceCreature);

        /// <summary>
        /// Get's called when npc faces specific direction.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        protected override void TurnedTo(int x, int y)
        {
            if (x == -1 && y == -1)
                RenderInformation.CancelScheduledUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.TurnTo);
            else
                RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.TurnTo);
        }

        /// <summary>
        /// Happens when specific text is being spoken by this character.
        /// </summary>
        /// <param name="text">Text which is being spoken.</param>
        protected override void TextSpoken(string text) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Speak);

        /// <summary>
        /// Get's called when specific hitsplat is about the rendered.
        /// </summary>
        /// <param name="splat">Splat which should be rendered.</param>
        protected override void HitSplatRendered(IHitSplat splat) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Hits);

        /// <summary>
        /// Hits the bar rendered.
        /// </summary>
        /// <param name="bar">The bar.</param>
        protected override void HitBarRendered(IHitBar bar) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Hits);

        /// <summary>
        /// Get's called when specific nonstandart movement is about to be rendered.
        /// </summary>
        /// <param name="movement">Movement which should be rendered.</param>
        protected override void NonstandardMovementRendered(IForceMovement movement) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.NonStandardMovement);

        /// <summary>
        /// Happens when specific glow is about to be rendered.
        /// </summary>
        /// <param name="glow">Glow which should be rendered.</param>
        protected override void GlowRendered(IGlow glow) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Npcs.UpdateFlags.Glow);

        /// <summary>
        /// Executes routine procedures.
        /// </summary>
        protected override void ContentTick()
        {
            Statistics.Tick();
            Script.Tick();
        }

        /// <summary>
        /// Executes routine procedures.
        /// </summary>
        protected override void UpdatesPrepareTick() => RenderInformation.Tick();

        /// <summary>
        /// Updates client by sending required packets.
        /// </summary>
        protected override Task UpdateTick()
        {
            if (Viewport.ShouldRebuild())
                Viewport.RebuildView();
            Viewport.UpdateTick();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reset's variables.
        /// </summary>
        protected override void ResetTick() => RenderInformation.Reset();
    }
}