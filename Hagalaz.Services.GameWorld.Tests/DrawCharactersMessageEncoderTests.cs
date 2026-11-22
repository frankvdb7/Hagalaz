using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Network.Protocol;
using Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Buffers;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class DrawCharactersMessageEncoderTests
{
    private static readonly Location Origin = Location.Create(3200, 3200, 0, 0);
    private static readonly Location Destination = Location.Create(3320, 3200, 0, 0);

    [TestMethod]
    public void Encode_WhileMapDrawIsDeferred_DoesNotRelocateTheLocalCharacter()
    {
        var deferred = Encode(Destination, lastLocation: Origin, teleported: true, unpublishedOrigin: Origin, deferredMapDraw: new DeferredMapDraw(false, false));
        var notMoved = Encode(Origin, lastLocation: Origin, teleported: false);

        CollectionAssert.AreEqual(notMoved, deferred);
    }

    [TestMethod]
    public void Encode_AfterTheMapDraw_SendsTheHeldMoveFromWhereTheClientLastSawTheCharacter()
    {
        // the character was already advanced to its destination by Reset while the draw was deferred
        var resumed = Encode(Destination, lastLocation: Destination, teleported: false, unpublishedOrigin: Origin);
        var directTeleport = Encode(Destination, lastLocation: Origin, teleported: true);

        CollectionAssert.AreEqual(directTeleport, resumed);
        CollectionAssert.AreNotEqual(Encode(Origin, lastLocation: Origin, teleported: false), resumed);
    }

    private static byte[] Encode(
        Location location,
        Location lastLocation,
        bool teleported,
        Location? unpublishedOrigin = null,
        DeferredMapDraw? deferredMapDraw = null)
    {
        var character = Substitute.For<ICharacter>();
        var renderInformation = Substitute.For<ICharacterRenderInformation>();
        var movement = Substitute.For<IMovement>();
        var viewport = Substitute.For<IViewport>();
        movement.Teleported.Returns(teleported);
        movement.Moved.Returns(teleported);
        renderInformation.IsInViewport(1).Returns(true); // the local character is always in its own viewport
        renderInformation.LastLocation.Returns(lastLocation);
        renderInformation.UnpublishedMoveOrigin.Returns(unpublishedOrigin);
        viewport.DeferredMapDraw.Returns(deferredMapDraw);
        character.Index.Returns(1);
        character.Location.Returns(location);
        character.Movement.Returns(movement);
        character.RenderInformation.Returns(renderInformation);
        character.Viewport.Returns(viewport);

        var encoder = new DrawCharactersMessageEncoder(Substitute.For<ICharacterLocationService>(), Substitute.For<ICharacterRenderMasksWriter>());
        using var buffer = MemoryBufferWriter.Get();
        encoder.EncodeMessage(new DrawCharactersMessage
        {
            Character = character,
            AllCharacters = new Dictionary<int, ICharacter> { [1] = character },
            LocalCharacters = new LinkedList<ICharacter>([character])
        }, new RaidoMessageBinaryWriter(buffer));
        return buffer.ToArray();
    }
}
