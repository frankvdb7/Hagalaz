namespace Hagalaz.Game.Abstractions.Model.Creatures;

public readonly record struct CreatureHandle<TCreature>(int Index, uint Generation)
    where TCreature : class, ICreature;
