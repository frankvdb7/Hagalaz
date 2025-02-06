using System;
using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Builders.Npc
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INpcOptional : INpcBuild
    {
        INpcOptional WithMinimumBounds(ILocation location);
        INpcOptional WithMaximumBounds(ILocation location);
        INpcOptional WithScript(INpcScript script);
        INpcOptional WithScript<TScript>() where TScript : INpcScript;
        INpcOptional WithScript(Type type);
        INpcOptional WithFaceDirection(DirectionFlag direction);
    }
}