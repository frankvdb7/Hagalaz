using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Messages;

namespace Hagalaz.Services.GameWorld.Services;

public sealed record WorldRegistrationUpdate(
    int WorldId,
    WorldOnlineMessage? ActiveMessage,
    bool IsAvailable,
    bool HasConflict,
    bool Changed);

public sealed class WorldRegistrationStore
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, WorldOnlineMessage>> _registrations = new();

    public WorldRegistrationUpdate ObserveOnline(WorldOnlineMessage message, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(message.InstanceId))
        {
            return new WorldRegistrationUpdate(message.Id, null, false, false, false);
        }

        var registrations = _registrations.GetOrAdd(message.Id, static _ => new ConcurrentDictionary<string, WorldOnlineMessage>());
        var changed = false;
        registrations.AddOrUpdate(message.InstanceId,
            _ =>
            {
                changed = true;
                return message;
            },
            (_, existing) =>
            {
                if (!IsNewer(message, existing))
                {
                    return existing;
                }

                changed = true;
                return message;
            });

        return GetUpdate(message.Id, now ?? DateTimeOffset.UtcNow, changed);
    }

    public WorldRegistrationUpdate ObserveOffline(WorldOfflineMessage message, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(message.InstanceId) || !_registrations.TryGetValue(message.Id, out var registrations))
        {
            return GetUpdate(message.Id, now ?? DateTimeOffset.UtcNow, false);
        }

        var removed = false;
        if (registrations.TryGetValue(message.InstanceId, out var existing) && existing.Generation == message.Generation)
        {
            removed = ((ICollection<KeyValuePair<string, WorldOnlineMessage>>)registrations)
                .Remove(new KeyValuePair<string, WorldOnlineMessage>(message.InstanceId, existing));
        }

        if (registrations.IsEmpty)
        {
            _registrations.TryRemove(message.Id, out _);
        }

        return GetUpdate(message.Id, now ?? DateTimeOffset.UtcNow, removed);
    }

    public IReadOnlyList<WorldRegistrationUpdate> Expire(DateTimeOffset? now = null)
    {
        var currentTime = now ?? DateTimeOffset.UtcNow;
        var updates = new List<WorldRegistrationUpdate>();
        foreach (var pair in _registrations)
        {
            var removed = false;
            foreach (var registration in pair.Value)
            {
                if (registration.Value.LeaseExpiresAt > currentTime)
                {
                    continue;
                }

                removed |= ((ICollection<KeyValuePair<string, WorldOnlineMessage>>)pair.Value).Remove(registration);
            }

            if (pair.Value.IsEmpty)
            {
                _registrations.TryRemove(pair.Key, out _);
            }

            if (removed)
            {
                updates.Add(GetUpdate(pair.Key, currentTime, true));
            }
        }

        return updates;
    }

    public bool HasConflict(int worldId, string localInstanceId, DateTimeOffset? now = null)
    {
        if (!_registrations.TryGetValue(worldId, out var registrations))
        {
            return false;
        }

        var currentTime = now ?? DateTimeOffset.UtcNow;
        return registrations.Values.Count(registration => registration.LeaseExpiresAt > currentTime) > 1 ||
               registrations.Values.Any(registration =>
                   registration.LeaseExpiresAt > currentTime && registration.InstanceId != localInstanceId);
    }

    public bool IsLocalGenerationAvailable(int worldId, string localInstanceId, DateTimeOffset? now = null)
    {
        var update = GetUpdate(worldId, now ?? DateTimeOffset.UtcNow, false);
        return update.IsAvailable && update.ActiveMessage!.InstanceId == localInstanceId;
    }

    private WorldRegistrationUpdate GetUpdate(int worldId, DateTimeOffset now, bool changed)
    {
        if (!_registrations.TryGetValue(worldId, out var registrations))
        {
            return new WorldRegistrationUpdate(worldId, null, false, false, changed);
        }

        var live = registrations.Values.Where(registration => registration.LeaseExpiresAt > now).ToArray();
        var hasConflict = live.Length > 1;
        return new WorldRegistrationUpdate(
            worldId,
            live.Length == 1 ? live[0] : null,
            live.Length == 1,
            hasConflict,
            changed);
    }

    private static bool IsNewer(WorldOnlineMessage candidate, WorldOnlineMessage current)
    {
        if (candidate.Generation != current.Generation)
        {
            return candidate.Generation > current.Generation;
        }

        if (candidate.StartedAt != current.StartedAt)
        {
            return candidate.StartedAt > current.StartedAt;
        }

        if (candidate.LastSeenAt != current.LastSeenAt)
        {
            return candidate.LastSeenAt > current.LastSeenAt;
        }

        return candidate.LeaseExpiresAt > current.LeaseExpiresAt;
    }
}
