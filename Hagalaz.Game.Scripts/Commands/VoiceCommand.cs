using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class VoiceCommand : IGameCommand
    {
        private readonly IAudioBuilder _soundBuilder;
        public string Name { get; } = "voice";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public VoiceCommand(IAudioBuilder soundBuilder) => _soundBuilder = soundBuilder;

        public async Task Execute(GameCommandArgs args)
        {
            await Task.CompletedTask;
            var cmd = args.Arguments;
            var sound = _soundBuilder.Create().AsVoice().WithId(int.Parse(cmd[1])).Build();
            args.Character.Session.SendMessage(sound.ToMessage());
        }
    }
}