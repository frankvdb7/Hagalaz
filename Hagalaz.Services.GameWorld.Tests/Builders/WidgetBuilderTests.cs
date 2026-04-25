using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Builders;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameWorld.Tests.Builders
{
    [TestClass]
    public class WidgetBuilderTests
    {
        private IWidgetScriptProvider _scriptProvider = null!;
        private ICharacter _character = null!;
        private IWidgetContainer _widgetContainer = null!;
        private IServiceProvider _serviceProvider = null!;
        private WidgetBuilder _builder = null!;

        [TestInitialize]
        public void Setup()
        {
            _scriptProvider = Substitute.For<IWidgetScriptProvider>();
            _character = Substitute.For<ICharacter>();
            _widgetContainer = Substitute.For<IWidgetContainer>();
            _serviceProvider = Substitute.For<IServiceProvider>();
            _character.Widgets.Returns(_widgetContainer);
            _character.ServiceProvider.Returns(_serviceProvider);
            _builder = new WidgetBuilder(_scriptProvider);
        }

        [TestMethod]
        public void Build_WhenNoFrameOpen_UsesZeroAsDefaultParentId()
        {
            // Arrange
            _widgetContainer.CurrentFrame.Returns((IWidget?)null);
            var script = Substitute.For<IWidgetScript>();

            var builder = _builder.Create()
                .ForCharacter(_character)
                .WithId(10)
                .WithScript(script);

            // Act
            var widget = builder.Build();

            // Assert
            Assert.IsNotNull(widget);
            Assert.AreEqual(0, widget.ParentId);
        }
    }
}
