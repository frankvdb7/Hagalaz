using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Class for npc rendering information.
    /// </summary>
    public class NpcRenderInformation : INpcRenderInformation
    {
        /// <summary>
        /// Contains owner of this information.
        /// </summary>
        private readonly Npc _owner;

        /// <summary>
        /// Contains current graphics.
        /// </summary>
        private readonly IGraphic[] _currentGraphics;

        /// <summary>
        /// Contains character update flag.
        /// </summary>
        public UpdateFlags UpdateFlag { get; private set; }

        /// <summary>
        /// Get's if character requires flag based update.
        /// </summary>
        public bool FlagUpdateRequired
        {
            get => UpdateFlag != 0;
            set
            {
                if (value)
                    throw new Exception("Can't set update required to true!");
                UpdateFlag = 0;
            }
        }

        /// <summary>
        /// Contains last location
        /// </summary>
        public ILocation LastLocation { get; private set; }

        /// <summary>
        /// Contains current animation.
        /// </summary>
        public IAnimation CurrentAnimation { get; private set; }

        /// <summary>
        /// Create's new rendering information instance.
        /// </summary>
        /// <param name="owner"></param>
        public NpcRenderInformation(Npc owner)
        {
            _owner = owner;
            _currentGraphics = new IGraphic[4];
        }

        /// <summary>
        /// Gets the currently active graphics.
        /// </summary>
        /// <returns>Returns a <code>Graphic</code> objects.</returns>
        public IGraphic GetCurrentGraphics(int id) => _currentGraphics[id];

        /// <summary>
        /// Initializes rendering information.
        /// </summary>
        public void OnRegistered() => LastLocation = _owner.Location.Clone();

        /// <summary>
        /// Tick's rendering information.
        /// </summary>
        public void Tick()
        {
            TakeAnimations();
            TakeGraphics();
        }

        /// <summary>
        /// Takes animations from queue.
        /// </summary>
        internal void TakeAnimations()
        {
            if (_owner.QueuedAnimationsCount > 0)
            {
                ScheduleFlagUpdate(UpdateFlags.Animation);
                CurrentAnimation = _owner.TakeAnimation();
                while (_owner.QueuedAnimationsCount > 0)
                {
                    var a = _owner.TakeAnimation();
                    if (a.Priority >= CurrentAnimation.Priority)
                        CurrentAnimation = a;
                }
            }
        }

        /// <summary>
        /// Takes graphics from queue.
        /// </summary>
        internal void TakeGraphics()
        {
            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[0] = _owner.TakeGraphic();
                ScheduleFlagUpdate(UpdateFlags.Graphic1);
            }

            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[1] = _owner.TakeGraphic();
                ScheduleFlagUpdate(UpdateFlags.Graphic2);
            }

            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[2] = _owner.TakeGraphic();
                ScheduleFlagUpdate(UpdateFlags.Graphic3);
            }

            if (_owner.QueuedGraphicsCount > 0)
            {
                _currentGraphics[3] = _owner.TakeGraphic();
                ScheduleFlagUpdate(UpdateFlags.Graphic4);
            }
        }


        /// <summary>
        /// Reset's information and updates global information.
        /// This method must be called after each update cycle.
        /// </summary>
        public void Reset()
        {
            FlagUpdateRequired = false;
            LastLocation = _owner.Location.Clone();
        }

        /// <summary>
        /// Shedule's flag based update to npc.
        /// </summary>
        /// <param name="flag"></param>
        public void ScheduleFlagUpdate(UpdateFlags flag) => UpdateFlag |= flag;

        /// <summary>
        /// Cancel's sheduled flag npc.
        /// </summary>
        /// <param name="flag"></param>
        public void CancelScheduledUpdate(UpdateFlags flag) => UpdateFlag &= ~flag;

        /// <summary>
        /// Get's if npc requires given flag
        /// based update.
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        internal bool RequiresFlagUpdate(UpdateFlags update) => (UpdateFlag & update) != 0;
    }
}