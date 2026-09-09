using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Factories;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Factories;

[TestClass]
public sealed class CharacterFactoryTests
{
    [TestMethod]
    public void Create_WhenCharacterConstructionFails_DisposesTheOwnedScope()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(Substitute.For<IServiceProvider>());
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        var factory = new CharacterFactory(serviceProvider);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            factory.Create(Substitute.For<IGameSession>(), Substitute.For<IGameClient>()));

        scope.Received(1).Dispose();
    }
}
