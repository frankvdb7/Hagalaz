using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Messages.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Characters_UpdateFlags = Hagalaz.Game.Abstractions.Model.Creatures.Characters.UpdateFlags;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class RenderingInformation
    /// </summary>
    public class CharacterRenderInformation : ICharacterRenderInformation
    {
        /// <summary>
        /// The character who owns the rendering information.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Contains information about world players.
        /// </summary>
        private readonly int[] _characterInformation = new int[2048];

        /// <summary>
        /// Contains current graphics.
        /// </summary>
        /// <value>The current graphics.</value>
        private readonly IGraphic[] _currentGraphics;

        private readonly ICharacterLocationService _characterLocationMap;
        private readonly ICharacterStore _characterStore;

        /// <summary>
        /// Contains local characters.
        /// </summary>
        /// <value>The local characters.</value>
        public LinkedList<ICharacter> LocalCharacters { get; set; }

        /// <summary>
        /// Contains local NPCS.
        /// </summary>
        /// <value>The local NPCS.</value>
        public LinkedList<INpc> LocalNpcs { get; set; }

        /// <summary>
        /// Contains last character location.
        /// </summary>
        /// <value>The last location.</value>
        public ILocation LastLocation { get; private set; } = default!;

        /// <summary>
        /// Gets a value indicating whether [large scene view].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [large scene view]; otherwise, <c>false</c>.
        /// </value>
        public bool LargeSceneView { get; }

        /// <summary>
        /// Contains character update flag.
        /// </summary>
        /// <value>The update flag.</value>
        public Characters_UpdateFlags UpdateFlag { get; private set; }

        /// <summary>
        /// Get's if character requires flag based update.
        /// </summary>
        /// <value><c>true</c> if [flag update required]; otherwise, <c>false</c>.</value>
        /// <exception cref="Exception"></exception>
        public bool FlagUpdateRequired
        {
            get => UpdateFlag != 0;
            private set
            {
                if (value)
                    throw new Exception("Can't set update required to true!");
                UpdateFlag = 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [item appearance update required].
        /// </summary>
        /// <value>
        /// <c>true</c> if [item appearance update required]; otherwise, <c>false</c>.
        /// </value>
        public bool ItemAppearanceUpdateRequired { get; private set; }

        /// <summary>
        /// Contains current animation.
        /// </summary>
        /// <value>The current animation.</value>
        public IAnimation CurrentAnimation { get; private set; }

        public CharacterRenderInformation(ICharacter renderable)
        {
            _owner = renderable;
            LocalCharacters = [];
            LocalNpcs = [];
            _currentGraphics = new IGraphic[4];
            _characterStore = renderable.ServiceProvider.GetRequiredService<ICharacterStore>();
            _characterLocationMap = renderable.ServiceProvider.GetRequiredService<ICharacterLocationService>();
        }

        /// <summary>
        /// Gets the currently active graphics.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Returns a <code>Graphic</code> objects.</returns>
        public IGraphic GetCurrentGraphics(int id) => _currentGraphics[id];

        /// <summary>
        /// Initializes rendering information.
        /// </summary>
        public void OnRegistered()
        {
            LastLocation = _owner.Location.Clone();
            LocalCharacters.AddLast(_owner); // at itself to local characters.
            SetInViewport(_owner.Index, true);
            ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.MovementType); // make yourself move normally
        }

        /// <summary>
        /// Tick's rendering information.
        /// </summary>
        public void Tick()
        {
            TakeAnimations();
            TakeGraphics();
        }

        /// <summary>
        /// Send's update packet to client.
        /// </summary>
        public async Task Update()
        {
            _owner.Session.SendMessage(new DrawCharactersMessage
            {
                Character = _owner,
                AllCharacters = await _characterStore.FindAllAsync().ToDictionaryAsync(c => c.Index, c => c),
                LocalCharacters = LocalCharacters
            });
             _owner.Session.SendMessage(new DrawNpcsMessage
            {
                Character = _owner,
                IsLargeSceneView = LargeSceneView,
                LocalNpcs = LocalNpcs,
                VisibleNpcs = _owner.Viewport.VisibleCreatures.OfType<INpc>().ToListHashSet()
            });
        }

        /// <summary>
        /// Reset's information and updates Global Player Information accordingly.
        /// This method must be called after each update cycle.
        /// </summary>
        public void Reset()
        {
            FlagUpdateRequired = false;
            ItemAppearanceUpdateRequired = false;
            for (var i = 1; i < 2048; i++)
            {
                SetJustCrossedViewport(i, false);
                SetIdle(i, IsIdleOnThisLoop(i));
                SetIdleOnThisLoop(i, false);
            }

            _characterLocationMap.SetLocationByIndex(_owner.Index, _owner.Location);
            LastLocation = _owner.Location.Clone();
        }

        /// <summary>
        /// Takes animations from queue.
        /// </summary>
        private void TakeAnimations()
        {
            if (_owner.QueuedAnimationsCount <= 0)
            {
                return;
            }

            ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Animation);
            CurrentAnimation = _owner.TakeAnimation();
            while (_owner.QueuedAnimationsCount > 0)
            {
                var a = _owner.TakeAnimation();
                if (a.Priority >= CurrentAnimation.Priority)
                    CurrentAnimation = a;
            }
        }

        /// <summary>
        /// Takes graphics from queue.
        /// </summary>
        private void TakeGraphics()
        {
            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[0] = _owner.TakeGraphic();
                ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic1);
            }

            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[1] = _owner.TakeGraphic();
                ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic2);
            }

            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[2] = _owner.TakeGraphic();
                ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic3);
            }

            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[3] = _owner.TakeGraphic();
                ScheduleFlagUpdate(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Graphic4);
            }
        }


        /// <summary>
        /// Get's if player is in screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if [is in viewport] [the specified index]; otherwise, <c>false</c>.</returns>
        public bool IsInViewport(int index) => (_characterInformation[index] & 0x1) != 0;

        /// <summary>
        /// Get's if player just showed up on screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool HasJustCrossedViewport(int index) => (_characterInformation[index] & 0x2) != 0;

        /// <summary>
        /// Set's if player is in screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void SetInViewport(int index, bool value)
        {
            if (value)
                _characterInformation[index] |= 0x1;
            else
                _characterInformation[index] &= ~0x1;
        }

        /// <summary>
        /// Set's if player just showed up on screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void SetJustCrossedViewport(int index, bool value)
        {
            if (value)
                _characterInformation[index] |= 0x2;
            else
                _characterInformation[index] &= ~0x2;
        }

        /// <summary>
        /// Set's that player skipped last cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void SetIdle(int index, bool value)
        {
            if (value)
                _characterInformation[index] |= 0x4;
            else
                _characterInformation[index] &= ~0x4;
        }

        /// <summary>
        /// Set's that player this cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void SetIdleOnThisLoop(int index, bool value)
        {
            if (value)
                _characterInformation[index] |= 0x8;
            else
                _characterInformation[index] &= ~0x8;
        }

        /// <summary>
        /// Get's if player skipped last cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if the specified index is idle; otherwise, <c>false</c>.</returns>
        public bool IsIdle(int index) => (_characterInformation[index] & 0x4) != 0;

        /// <summary>
        /// Get's if player skipped this cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if [is idle on this loop] [the specified index]; otherwise, <c>false</c>.</returns>
        public bool IsIdleOnThisLoop(int index) => (_characterInformation[index] & 0x8) != 0;

        /// <summary>
        /// Schedules flag based update to character.
        /// </summary>
        /// <param name="flag">The flag.</param>
        public void ScheduleFlagUpdate(Characters_UpdateFlags flag) => UpdateFlag |= flag;

        /// <summary>
        /// Cancel's scheduled flag update.
        /// </summary>
        /// <param name="flag">The flag.</param>
        public void CancelScheduledUpdate(Characters_UpdateFlags flag) => UpdateFlag &= ~flag;

        /// <summary>
        /// Schedules the item appearance update.
        /// </summary>
        public void ScheduleItemAppearanceUpdate() => ItemAppearanceUpdateRequired = true;
    }
}