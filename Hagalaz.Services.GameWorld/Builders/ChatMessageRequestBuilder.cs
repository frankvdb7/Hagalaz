using System;
using Hagalaz.Game.Abstractions.Builders.Request;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class ChatMessageRequestBuilder : IChatMessageRequestBuilder, IChatMessageRequestOption, IChatMessageRequestTarget, IChatMessageRequestType,
        IChatMessageRequestSource, IChatMessageRequestSourceMessage, IChatMessageRequestTargetMessage, IChatMessageRequestBuild
    {
        private class ChatMessageRequestOptionBuilder : IChatMessageRequestOptionBuilder
        {
            public CharacterClickType ClickType { get; private set; }
            public Action Action { get; private set; } = default!;
            public string Name { get; private set; } = default!;

            public IChatMessageRequestOptionBuilder WithType(CharacterClickType clickType)
            {
                ClickType = clickType;
                return this;
            }

            public IChatMessageRequestOptionBuilder WithAction(Action action)
            {
                Action = action;
                return this;
            }

            public IChatMessageRequestOptionBuilder WithName(string name)
            {
                Name = name;
                return this;
            }
        }

        private ICharacter Source { get; set; } = default!;

        private string SourceMessage { get; set; } = default!;

        private ICharacter Target { get; set; } = default!;

        private string TargetMessage { get; set; } = default!;

        private ChatMessageType Type { get; set; } = default!;

        private ChatMessageRequestOptionBuilder Option { get; set; } = default!;

        public IChatMessageRequestSource Create() => new ChatMessageRequestBuilder();

        public IChatMessageRequest Build()
        {
            var request = new ChatMessageRequest
            {
                Source = Source,
                SourceMessage = SourceMessage,
                Target = Target,
                TargetMessage = TargetMessage,
                Type = Type
            };
            request.SetCharacterOptionHandler(Option.ClickType, Option.Name, Option.Action);
            return request;
        }

        public IChatMessageRequestBuild WithOption(Action<IChatMessageRequestOptionBuilder> optionBuilder)
        {
            var builder = new ChatMessageRequestOptionBuilder();
            optionBuilder(builder);
            Option = builder;
            return this;
        }

        public IChatMessageRequestTargetMessage WithTarget(ICharacter character)
        {
            Target = character;
            return this;
        }

        public IChatMessageRequestOption WithType(ChatMessageType type)
        {
            Type = type;
            return this;
        }

        public IChatMessageRequestSourceMessage WithSource(ICharacter character)
        {
            Source = character;
            return this;
        }

        public IChatMessageRequestTarget WithSourceMessage(string message)
        {
            SourceMessage = message;
            return this;
        }

        public IChatMessageRequestType WithTargetMessage(string message)
        {
            TargetMessage = message;
            return this;
        }
    }
}