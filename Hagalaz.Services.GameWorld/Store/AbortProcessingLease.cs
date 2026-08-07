using System;

namespace Hagalaz.Services.GameWorld.Store;

/// <summary>
/// Identifies one owner of a pending session-abort reservation.
/// </summary>
public readonly record struct AbortProcessingLease(Guid Token, DateTimeOffset StartedAtUtc);
