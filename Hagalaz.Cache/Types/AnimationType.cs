using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Types
{
    /// <summary>
    /// Represents creature animation definition.
    /// </summary>
    public class AnimationType : IAnimationType
    {
        /// <summary>
        /// Contains ID of this animation.
        /// </summary>
        /// <value>The ID.</value>
        public int Id { get; }
        /// <summary>
        /// Gets the extra data.
        /// </summary>
        /// <value>
        /// The extra data.
        /// </value>
        public Dictionary<int, object>? ExtraData { get; internal set; }
        /// <summary>
        /// Gets an int399.
        /// </summary>
        /// <value>An int399.</value>
        public int Loop { get; internal set; }
        /// <summary>
        /// Gets an int array400.
        /// </summary>
        /// <value>An int array400.</value>
        public int[]? MaxSoundSpeed { get; internal set; }
        /// <summary>
        /// Gets the weapon displayed.
        /// </summary>
        /// <value>The weapon displayed.</value>
        public int WeaponDisplayedItemId { get; internal set; }
        /// <summary>
        /// Gets an int array402.
        /// </summary>
        /// <value>An int array402.</value>
        public int[]? Frames { get; internal set; }
        /// <summary>
        /// Gets an int403.
        /// </summary>
        /// <value>An int403.</value>
        public int Cycles { get; internal set; }
        /// <summary>
        /// Gets an int404.
        /// </summary>
        /// <value>An int404.</value>
        public int AnimationMode { get; internal set; }
        /// <summary>
        /// Gets an int array405.
        /// </summary>
        /// <value>An int array405.</value>
        public int[]? Delays { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this instance is tweened.
        /// </summary>
        /// <value><c>true</c> if this instance is tweened; otherwise, <c>false</c>.</value>
        public bool IsTweened { get; internal set; }
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean409].
        /// </summary>
        /// <value><c>true</c> if [a boolean409]; otherwise, <c>false</c>.</value>
        public bool ABoolean409 { get; internal set; }
        /// <summary>
        /// Gets an int410.
        /// </summary>
        /// <value>An int410.</value>
        public int ReplayMode { get; internal set; }
        /// <summary>
        /// Gets the shield displayed.
        /// </summary>
        /// <value>The shield displayed.</value>
        public int ShieldDisplayedItemId { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [a boolean413].
        /// </summary>
        /// <value><c>true</c> if [a boolean413]; otherwise, <c>false</c>.</value>
        public bool ABoolean413 { get; internal set; }
        /// <summary>
        /// Gets a boolean array414.
        /// </summary>
        /// <value>A boolean array414.</value>
        public bool[]? ABooleanArray414 { get; internal set; }
        /// <summary>
        /// Gets an int array415.
        /// </summary>
        /// <value>An int array415.</value>
        public int[]? MinSoundSpeed { get; internal set; }
        /// <summary>
        /// Gets an int array416.
        /// </summary>
        /// <value>An int array416.</value>
        public int[]? SoundVolumes { get; internal set; }
        /// <summary>
        /// Gets the walking properties.
        /// </summary>
        /// <value>The walking properties.</value>
        public int WalkingProperties { get; internal set; }
        /// <summary>
        /// Gets an int array array418.
        /// </summary>
        /// <value>An int array array418.</value>
        public int[][]? SoundSettings { get; internal set; }
        /// <summary>
        /// Gets an int array419.
        /// </summary>
        /// <value>An int array419.</value>
        public int[]? InterfaceFrames { get; internal set; }

        /// <summary>
        /// Construct's new animation definition.
        /// </summary>
        /// <param name="id">ID of the animation.</param>
        public AnimationType(int id)
        {
            Id = id;
            Loop = -1;
            AnimationMode = -1;
            Cycles = 99;
            ReplayMode = 2;
            ABoolean413 = false;
            ABoolean409 = false;
            WalkingProperties = -1;
            Priority = 5;
            IsTweened = false;
            WeaponDisplayedItemId = -1;
            ShieldDisplayedItemId = -1;
        }

        /// <summary>
        /// Set's some information about animation.
        /// </summary>
        public void AfterDecode()
        {
            if (AnimationMode == -1)
            {
                if (ABooleanArray414 != null)
                    AnimationMode = 2;
                else
                    AnimationMode = 0;
            }
            if (WalkingProperties == -1)
            {
                if (ABooleanArray414 != null)
                    WalkingProperties = 2;
                else
                    WalkingProperties = 0;
            }
        }

        /// <summary>
        /// Estimated duration in server ticks.
        /// </summary>
        /// <returns></returns>
        public int EstimatedDurationInServerTicks() => (EstimatedDurationInClientTicks() * 30) / 1000;

        /// <summary>
        /// Get's estimated duration in client ticks.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int EstimatedDurationInClientTicks()
        {
            if (Delays == null)
                return -1;

            return Delays.Sum();
        }

    }
}
