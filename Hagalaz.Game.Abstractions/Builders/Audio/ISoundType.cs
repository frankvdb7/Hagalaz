using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.Audio
{
    /// <summary>
    /// Represents the step in the fluent builder pattern for creating an audio object
    /// where the specific type of sound (e.g., sound effect, music, voice) must be chosen.
    /// </summary>
    /// <remarks>
    /// This interface is not intended for direct implementation. It is part of the fluent API provided by <see cref="IAudioBuilder"/>.
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ISoundType
    {
        /// <summary>
        /// Specifies that the audio object to be built is a standard sound effect.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the sound effect ID.</returns>
        ISoundId AsSound();

        /// <summary>
        /// Specifies that the audio object to be built is a music effect.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the music effect ID.</returns>
        IMusicEffectId AsMusicEffect();

        /// <summary>
        /// Specifies that the audio object to be built is a voice-over or character sound.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the voice sound ID.</returns>
        IVoiceId AsVoice();
    }
}