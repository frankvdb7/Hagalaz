using System;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Model.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Builders
{
    public class WidgetBuilder : IWidgetBuilder, IWidgetId, IWidgetOptional, IWidgetCharacter
    {
        private readonly IWidgetScriptProvider _iWidgetScriptProvider;
        private int _id;
        private int _transparency;
        private int? _parentId;
        private int? _parentSlot;
        private IWidgetScript? _script;
        private Type? _scriptType;
        private ICharacter _character = default!;
        private bool _isFrame;

        public WidgetBuilder(IWidgetScriptProvider iWidgetScriptProvider) => _iWidgetScriptProvider = iWidgetScriptProvider;
        public IWidgetCharacter Create() => new WidgetBuilder(_iWidgetScriptProvider);

        public IWidgetOptional WithId(int id)
        {
            _id = id;
            return this;
        }

        public IWidget Build()
        {
            if (_script == null)
            {
                var scriptType = _scriptType ?? _iWidgetScriptProvider.FindScriptTypeById(_id);
                _script = (IWidgetScript)_character.ServiceProvider.GetRequiredService(scriptType);
            }

            IWidget widget;
            if (_isFrame)
            {
                widget = new Widget(_character, _id, _transparency, _script);
            }
            else
            {
                _parentId ??= _character.Widgets.CurrentFrame.Id;
                _parentSlot ??= 0;
                widget = new Widget(_character, _id, _parentId.Value, _parentSlot.Value, _transparency, _script);
            }
            return widget;
        }

        public void Open()
        {
            var widget = Build();
            _character.Widgets.OpenWidget(widget);
        }

        public IWidgetOptional WithTransparency(int transparency)
        {
            _transparency = transparency;
            return this;
        }

        public IWidgetOptional WithParentId(int parentId)
        {
            _parentId = parentId;
            return this;
        }

        public IWidgetOptional WithParentSlot(int parentSlot)
        {
            _parentSlot = parentSlot;
            return this;
        }

        public IWidgetOptional WithScript(IWidgetScript script)
        {
            _script = script;
            return this;
        }

        public IWidgetOptional WithScript<TScript>() where TScript : IWidgetScript
        {
            _scriptType = typeof(TScript);
            return this;
        }

        public IWidgetOptional AsFrame()
        {
            _isFrame = true;
            return this;
        }

        public IWidgetId ForCharacter(ICharacter character)
        {
            _character = character;
            return this;
        }
    }
}