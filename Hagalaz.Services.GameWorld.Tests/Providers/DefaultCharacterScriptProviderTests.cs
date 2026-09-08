using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Providers;

[TestClass]
public sealed class DefaultCharacterScriptProviderTests
{
    [TestMethod]
    public void GetAllScripts_DoesNotConstructContextDependentScriptBeforeContextAssignment()
    {
        var contextProvider = new CharacterContextAccessor();
        var character = Substitute.For<ICharacter>();
        var characterContext = Substitute.For<ICharacterContext>();
        characterContext.Character.Returns(character);

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<ICharacterContextProvider>(contextProvider)
            .AddSingleton<ICharacterContextAccessor>(contextProvider)
            .AddScoped<ContextDependentDefaultCharacterScript>()
            .AddScoped<IDefaultCharacterScript>(services =>
                services.GetRequiredService<ContextDependentDefaultCharacterScript>())
            .AddScoped<DefaultCharacterScriptProvider>()
            .BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var scriptProvider = scope.ServiceProvider.GetRequiredService<DefaultCharacterScriptProvider>();

        contextProvider.Context = characterContext;

        var scripts = scriptProvider.GetAllScripts().ToList();

        Assert.AreEqual(1, scripts.Count);
        Assert.AreSame(character, scripts[0].Character);
    }

    private sealed class ContextDependentDefaultCharacterScript : IDefaultCharacterScript
    {
        private readonly ICharacter _character;

        public ContextDependentDefaultCharacterScript(ICharacterContextAccessor contextAccessor) =>
            _character = contextAccessor.Context?.Character
                ?? throw new InvalidOperationException("The character context must be assigned before script construction.");

        public ICharacter Character => _character;

        public bool CanBeLootedBy(ICreature killer) => true;

        public bool CanAttack(ICreature target) => true;

        public bool CanBeAttackedBy(ICreature attacker) => true;

        public void OnDeath() { }

        public void OnKilledBy(ICreature killer) { }

        public void OnTargetKilled(ICreature target) { }

        public void OnSpawn() { }

        public void Tick() { }

        public void OnInterrupt(object source) { }

        public void OnRegistered() { }

        public void OnDestroy() { }

        public bool CanRenderSkull(SkullIcon icon) => true;

        public bool IsBusy() => false;

        public void OnRemove() { }
    }
}
