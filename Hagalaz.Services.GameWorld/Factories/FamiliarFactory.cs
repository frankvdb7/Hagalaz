using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Characters.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Factories;

/// <summary>
/// Composes familiar NPCs and applies their restoration state at the application boundary.
/// </summary>
public sealed class FamiliarFactory(
    INpcBuilder npcBuilder,
    IFamiliarScriptProvider familiarScriptProvider,
    ISummoningDefinitionStore summoningDefinitionStore,
    FamiliarRestorationState restorationState) : IFamiliarFactory
{
    public INpcHandle Spawn(ICharacter summoner, SummoningDto definition)
    {
        IFamiliarScript? script = null;

        try
        {
            return Configure(definition, summoner, (activator, owner) =>
            {
                script = CreateScript(summoner, owner, definition, activator);
                return script;
            }).Spawn();
        }
        catch
        {
            DetachFamiliar(summoner, script);
            throw;
        }
    }

    public bool TryRestore(ICharacter summoner)
    {
        if (restorationState.FamiliarId is not { } familiarId)
        {
            return false;
        }

        var definition = summoningDefinitionStore.FindByNpcId(familiarId);
        if (definition is null)
        {
            restorationState.Clear();
            return false;
        }

        IFamiliarScript? restoredScript = null;
        INpcHandle? handle = null;

        try
        {
            handle = Configure(definition, summoner, (activator, owner) =>
            {
                restoredScript = CreateScript(summoner, owner, definition, activator);
                return restoredScript;
            }).Spawn();

            if (restoredScript is null)
            {
                throw new InvalidOperationException("The familiar NPC was not composed during registration.");
            }

            ApplyRestoredState(restoredScript);
            restorationState.Clear();
            return true;
        }
        catch
        {
            try
            {
                handle?.Unregister();
            }
            finally
            {
                DetachFamiliar(summoner, restoredScript);
                restorationState.Clear();
            }

            throw;
        }
    }

    private INpcOptional Configure(
        SummoningDto definition,
        ICharacter summoner,
        Func<INpcScriptActivator, INpc, INpcScript> scriptFactory) =>
        npcBuilder.Create()
            .WithId(definition.NpcId)
            .WithLocation(summoner.Location)
            .WithScript(scriptFactory);

    private IFamiliarScript CreateScript(
        ICharacter summoner,
        INpc owner,
        SummoningDto definition,
        INpcScriptActivator activator)
    {
        var script = Activate(summoner, owner, definition, activator);
        summoner.AttachFamiliar(script, definition.NpcId);
        return script;
    }

    private void ApplyRestoredState(IFamiliarScript script)
    {
        if (restorationState.Familiar is not null && script is IHydratable<HydratedFamiliar> hydratable)
        {
            hydratable.Hydrate(restorationState.Familiar);
        }

        if (restorationState.Inventory is not null && script is IHydratable<IReadOnlyList<HydratedItem>> inventory)
        {
            inventory.Hydrate(restorationState.Inventory);
        }
    }

    private static void DetachFamiliar(ICharacter summoner, IFamiliarScript? script)
    {
        if (script is not null)
        {
            summoner.DetachFamiliar(script.Familiar);
        }
    }

    private IFamiliarScript Activate(
        ICharacter summoner,
        INpc owner,
        SummoningDto definition,
        INpcScriptActivator activator)
    {
        var scriptType = familiarScriptProvider.FindFamiliarScriptTypeById(definition.NpcId);
        var script = (IFamiliarScript)activator.Create(scriptType, owner);
        script.AttachToSummoner(summoner, definition);
        return script;
    }
}
