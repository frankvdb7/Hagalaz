using System;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    public class ChatMessageRequest : IChatMessageRequest
    {
        private class Option
        {
            public required string Name { get; init; }

            public required CharacterClickType ClickType { get; init; }

            public required CharacterOptionClicked Handler { get; init; }
        }

        private Option? _option;

        public required ICharacter Source { get; init; }

        public required string SourceMessage { get; init; }

        public required ICharacter Target { get; init; }

        public required string TargetMessage { get; init; }

        public required ChatMessageType Type { get; init; }

        public void SetCharacterOptionHandler(CharacterClickType clickType, string optionName, Action callback) =>
            _option = new Option
            {
                Name = optionName,
                ClickType = clickType,
                Handler = (source, forceRun) =>
                {
                    Target.Interrupt(this);
                    Target.Movement.MovementType = Target.Movement.MovementType == MovementType.Run || forceRun ? MovementType.Run : MovementType.Walk;
                    Target.QueueTask(new CreatureReachTask(Target, source, (success) =>
                    {
                        Target.Interrupt(this);
                        if (success)
                        {
                            if (source.IsBusy())
                                Target.SendChatMessage("The other player is busy at the moment.");
                            else
                            {
                                if (source == Source)
                                {
                                    source.Interrupt(this);
                                    callback.Invoke();
                                }

                                Target.UnregisterCharactersOptionHandler(clickType);
                            }
                        }
                        else
                            Target.SendChatMessage(GameStrings.YouCantReachThat);
                    }));
                }
            };

        public void TrySend() =>
            Source.QueueTask(new CreatureReachTask(Source, Target, (success) =>
            {
                Source.Interrupt(this);
                if (success)
                {
                    if (Source.IsBusy())
                        Target.SendChatMessage("The other player is busy at the moment.");
                    else
                    {
                        Source.SendChatMessage(SourceMessage);
                        Target.SendChatMessage(TargetMessage, Type, Source.DisplayName, Source.PreviousDisplayName);
                        if (_option != null)
                        {
                            Target.RegisterCharactersOptionHandler(_option.ClickType, _option.Name, ushort.MaxValue, false, _option.Handler);
                        }
                    }
                }
                else
                    Source.SendChatMessage(GameStrings.YouCantReachThat);
            }));
    }
}