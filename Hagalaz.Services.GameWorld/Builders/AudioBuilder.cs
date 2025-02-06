using System;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model.Sound;
using Hagalaz.Services.GameWorld.Model.Audio;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class AudioBuilder : IAudioBuilder, ISoundBuild, ISoundId, ISoundOptional, ISoundType, IMusicEffectId, IMusicEffectOptional, IVoiceId, IVoiceOptional
    {
        /// <summary>
        /// Enum SoundType
        /// </summary>
        private enum SoundType
        {
            /// <summary>
            /// A normal sound.
            /// </summary>
            Normal,

            /// <summary>
            /// A voice sound.
            /// </summary>
            Voice,

            /// <summary>
            /// A music effect located on the music cache indice.
            /// </summary>
            MusicEffect,
        }

        private SoundType _soundType;
        private int _id;
        private int _volume = 255;
        private int _repeatCount = 1;
        private int _delay;
        private int _playbackSpeed = 256;

        public ISoundType Create() => new AudioBuilder();

        public ISound Build()
        {
            switch (_soundType)
            {
                case SoundType.Normal:
                    return new Sound
                    {
                        Id = _id,
                        RepeatCount = _repeatCount,
                        Volume = _volume,
                        PlaybackSpeed = _playbackSpeed,
                        Delay = _delay
                    };
                case SoundType.Voice:
                    return new Voice
                    {
                        Id = _id,
                        Delay = _delay,
                        RepeatCount = _repeatCount,
                        Volume = _volume
                    };
                case SoundType.MusicEffect:
                    return new MusicEffect
                    {
                        Id = _id,
                        Volume = _volume
                    };
                default:
                    throw new NotImplementedException($"${nameof(SoundType)} '{_soundType}' does not exist.");
            }
        }

        IMusicEffectOptional IMusicEffectOptional.WithVolume(int volume)
        {
            _volume = volume;
            return this;
        }

        IVoiceOptional IVoiceOptional.WithRepeatCount(int count)
        {
            _repeatCount = count;
            return this;
        }

        IVoiceOptional IVoiceOptional.WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }


        public ISoundId AsSound()
        {
            _soundType = SoundType.Normal;
            return this;
        }

        public IMusicEffectId AsMusicEffect()
        {
            _soundType = SoundType.MusicEffect;
            return this;
        }

        public IVoiceId AsVoice()
        {
            _soundType = SoundType.Voice;
            return this;
        }

        ISoundOptional ISoundId.WithId(int id)
        {
            _id = id;
            return this;
        }

        ISoundOptional ISoundOptional.WithVolume(int volume)
        {
            _volume = volume;
            return this;
        }

        IVoiceOptional IVoiceOptional.WithVolume(int volume)
        {
            _volume = volume;
            return this;
        }

        ISoundOptional ISoundOptional.WithRepeatCount(int count)
        {
            _repeatCount = count;
            return this;
        }

        ISoundOptional ISoundOptional.WithDelay(int delay)
        {
            _delay = delay;
            return this;
        }

        public ISoundOptional WithPlaybackSpeed(int playbackSpeed)
        {
            _playbackSpeed = playbackSpeed;
            return this;
        }

        IMusicEffectOptional IMusicEffectId.WithId(int id)
        {
            _id = id;
            return this;
        }

        IVoiceOptional IVoiceId.WithId(int id)
        {
            _id = id;
            return this;
        }
    }
}