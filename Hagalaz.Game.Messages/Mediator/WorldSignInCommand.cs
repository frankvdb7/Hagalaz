using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Messages.Mediator
{
    public record WorldSignInCommand(ICharacter Character);
}
