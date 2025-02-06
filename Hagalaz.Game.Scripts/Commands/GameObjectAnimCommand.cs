using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Commands
{
    public class GameObjectAnimCommand : IGameCommand
    {
        private readonly IAnimationBuilder _animationBuilder;
        public string Name { get; } = "objanim";
        public Permission Permission { get; } = Permission.SystemAdministrator;

        public GameObjectAnimCommand(IAnimationBuilder animationBuilder)
        {
            _animationBuilder = animationBuilder;
        }

        public async Task Execute(GameCommandArgs args)
        {
            await Task.CompletedTask;
            var cmd = args.Arguments;
            var objectID = int.Parse(cmd[1]);
            var animID = int.Parse(cmd[2]);
            var gameObjectService = args.Character.ServiceProvider.GetRequiredService<IGameObjectService>();
            var visible = gameObjectService.FindAll(new GameObjectFindAll
                {
                    Location = args.Character.Location, Id = objectID
                })
                .FirstOrDefault();
            if (visible != null)
            {
                gameObjectService.AnimateGameObject(visible, _animationBuilder.Create().WithId(animID).Build());
            }
        }
    }
}