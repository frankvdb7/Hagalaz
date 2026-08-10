using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets.Tabs;

[TestClass]
public sealed class SpellBookTabTests
{
    [TestMethod]
    public void ResolveThroughDependencyInjection_WithExplicitProjectileBuilder_DoesNotRequireGlobalInitialization()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => Substitute.For<ICharacterContextAccessor>());
        services.AddSingleton(_ => Substitute.For<IScopedGameMediator>());
        services.AddSingleton(_ => Substitute.For<IMagicService>());
        services.AddSingleton(_ => Substitute.For<IItemBuilder>());
        services.AddSingleton(_ => Substitute.For<IProjectileBuilder>());
        services.AddTransient<SpellBookTab>();

        using var provider = services.BuildServiceProvider();

        var script = provider.GetRequiredService<SpellBookTab>();

        Assert.IsNotNull(script);
    }
}
