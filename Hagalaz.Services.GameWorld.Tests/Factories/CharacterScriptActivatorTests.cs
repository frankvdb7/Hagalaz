using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Services.GameWorld.Factories;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Factories;

[TestClass]
public sealed class CharacterScriptActivatorTests
{
    [TestMethod]
    public void CharacterScriptActivator_ResolvesFromProvidedScope()
    {
        var rootScript = Substitute.For<ICharacterScript>();
        var scopedScript = Substitute.For<ICharacterScript>();
        using var provider = new ServiceCollection()
            .AddSingleton(rootScript)
            .AddScoped<ICharacterScript>(_ => scopedScript)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = new CharacterScriptActivator(scope.ServiceProvider).Create<ICharacterScript>();

        Assert.AreSame(scopedScript, result);
        Assert.AreNotSame(rootScript, result);
    }

    [TestMethod]
    public void CharacterNpcScriptActivator_ResolvesTypeFromProvidedScope()
    {
        var rootScript = Substitute.For<ICharacterNpcScript>();
        var scopedScript = Substitute.For<ICharacterNpcScript>();
        using var provider = new ServiceCollection()
            .AddSingleton(rootScript)
            .AddScoped<ICharacterNpcScript>(_ => scopedScript)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = new CharacterNpcScriptActivator(scope.ServiceProvider).Create(typeof(ICharacterNpcScript));

        Assert.AreSame(scopedScript, result);
        Assert.AreNotSame(rootScript, result);
    }

    [TestMethod]
    public void WidgetScriptActivator_ResolvesFromCharacterScope()
    {
        var rootScript = Substitute.For<IWidgetScript>();
        var scopedScript = Substitute.For<IWidgetScript>();
        using var provider = new ServiceCollection()
            .AddSingleton(rootScript)
            .AddScoped<IWidgetScript>(_ => scopedScript)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        var character = Substitute.For<ICharacter>();
        character.ServiceProvider.Returns(scope.ServiceProvider);

        var result = new WidgetScriptActivator().Create(character, typeof(IWidgetScript));

        Assert.AreSame(scopedScript, result);
        Assert.AreNotSame(rootScript, result);
    }

}
