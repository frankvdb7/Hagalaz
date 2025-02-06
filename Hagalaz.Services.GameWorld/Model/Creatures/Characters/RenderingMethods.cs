using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Contains methods for managing character regions.
    /// </summary>
    public partial class Character : Creature
    {
        /// <summary>
        /// Informs character that it's movement type have been changed.
        /// </summary>
        /// <param name="newType">The newtype.</param>
        /// <exception cref="System.Exception"></exception>
        public override void MovementTypeChanged(MovementType newType)
        {
            if (newType == MovementType.WalkingBackwards)
                throw new Exception("Unsupported movement type.");
            RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.MovementType);
        }

        /// <summary>
        /// Inform's client that specific movement type was enabled for this tick only.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="System.Exception"></exception>
        public override void TemporaryMovementTypeEnabled(MovementType type)
        {
            if (type == MovementType.WalkingBackwards)
                throw new Exception("Unsupported temporary movement type.");
            RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.TemporaryMovementType);
        }

        /// <summary>
        /// Get's called when character faces specific character.
        /// If character is null then character is not facing anything anymore.
        /// </summary>
        /// <param name="creature">The creature.</param>
        protected override void CreatureFaced(ICreature? creature) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.FaceCreature);

        /// <summary>
        /// Get's called when character faces specific direction.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        protected override void TurnedTo(int x, int y)
        {
            if (x == -1 && y == -1)
                RenderInformation.CancelScheduledUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.TurnTo);
            else
                RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.TurnTo);
        }

        /// <summary>
        /// Happens when specific text is being spoken by this character.
        /// </summary>
        /// <param name="text">Text which is being spoken.</param>
        protected override void TextSpoken(string text) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Speak);

        /// <summary>
        /// Get's called when specific hitsplat is about the rendered.
        /// </summary>
        /// <param name="splat">Splat which should be rendered.</param>
        protected override void HitSplatRendered(IHitSplat splat) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Hits);

        /// <summary>
        /// Hits the bar rendered.
        /// </summary>
        /// <param name="bar">The bar.</param>
        protected override void HitBarRendered(IHitBar bar) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Hits);

        /// <summary>
        /// Get's called when specific nonstandart movement is about to be rendered.
        /// </summary>
        /// <param name="movement">Movement which should be rendered.</param>
        protected override void NonstandardMovementRendered(IForceMovement movement) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.NonStandardMovement);

        /// <summary>
        /// Get's called when specific glow is about to be rendered.
        /// </summary>
        /// <param name="glow">Glow which should be rendered.</param>
        protected override void GlowRendered(IGlow glow) => RenderInformation.ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Glow);

        /// <summary>
        /// Get's if this character should be rendered for given character.
        /// </summary>
        /// <param name="viewer">Character for which this character should be rendered.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool ShouldBeRenderedFor(ICharacter viewer) => Appearance.Visible || viewer == this;

        /// <summary>
        /// Get's if this character should be rendered for given npc.
        /// </summary>
        /// <param name="viewer">NPC for which this character should be rendered.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool ShouldBeRenderedFor(INpc viewer) => Appearance.Visible;

        /// <summary>
        /// Executes routine procedures.
        /// </summary>
        protected override void ContentTick() => Tick();

        /// <summary>
        /// Executes routine procedures.
        /// </summary>
        protected override void UpdatesPrepareTick() => RenderInformation.Tick();

        /// <summary>
        /// Updates client by sending required packets.
        /// </summary>
        protected override async Task UpdateTick()
        {
            if (Viewport.ShouldRebuild())
                await UpdateMapAsync(false);
            Viewport.UpdateTick();
            await RenderInformation.Update();
        }

        /// <summary>
        /// Reset's variables.
        /// </summary>
        protected override void ResetTick() => RenderInformation.Reset();
    }
}