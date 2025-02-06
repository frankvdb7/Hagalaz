using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SoundCommand : IGameCommand
    {
        private readonly IAudioBuilder _soundBuilder;
        public string Name { get; } = "sound";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public SoundCommand(IAudioBuilder soundBuilder) => _soundBuilder = soundBuilder;

        public async Task Execute(GameCommandArgs args)
        {
            await Task.CompletedTask;
            var cmd = args.Arguments;
            var id = int.Parse(cmd[1]);
            var volume = 255;
            if (cmd.Length > 2)
                volume = int.Parse(cmd[2]);
            var delay = 0;
            if (cmd.Length > 3)
                delay = int.Parse(cmd[3]);
            var speed = 256;
            if (cmd.Length > 4)
                speed = int.Parse(cmd[4]);
            var timesToPlay = 1;
            if (cmd.Length > 5)
                timesToPlay = int.Parse(cmd[5]);
            var sound = _soundBuilder.Create().AsSound().WithId(id).WithVolume(volume).WithDelay(delay).WithPlaybackSpeed(speed).WithRepeatCount(timesToPlay).Build();
            args.Character.Session.SendMessage(sound.ToMessage());
        }
    }
}