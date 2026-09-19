using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Services.Contacts.Store.Model;

namespace Hagalaz.Services.Contacts.Store;

public sealed record WorldSessionUpdate(
    int WorldId,
    WorldSessionContext? ActiveSession,
    bool IsAvailable,
    bool Changed,
    WorldSessionContext? RemovedSession = null);

public sealed class WorldSessionStore
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, WorldSessionContext>> _sessions = new();

    public WorldSessionUpdate ObserveOnline(WorldSessionContext session, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(session.InstanceId))
        {
            return new WorldSessionUpdate(session.WorldId, null, false, false);
        }

        var sessions = _sessions.GetOrAdd(session.WorldId, static _ => new ConcurrentDictionary<string, WorldSessionContext>());
        var changed = false;
        sessions.AddOrUpdate(session.InstanceId,
            _ =>
            {
                changed = true;
                return session;
            },
            (_, existing) =>
            {
                if (!IsNewer(session, existing))
                {
                    return existing;
                }

                changed = true;
                return session;
            });

        return GetUpdate(session.WorldId, now ?? DateTimeOffset.UtcNow, changed);
    }

    public WorldSessionUpdate ObserveOffline(int worldId, string instanceId, long generation, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || !_sessions.TryGetValue(worldId, out var sessions))
        {
            return GetUpdate(worldId, now ?? DateTimeOffset.UtcNow, false);
        }

        var removed = false;
        WorldSessionContext? removedSession = null;
        if (sessions.TryGetValue(instanceId, out var existing) && existing.Generation == generation)
        {
            removed = ((ICollection<KeyValuePair<string, WorldSessionContext>>)sessions)
                .Remove(new KeyValuePair<string, WorldSessionContext>(instanceId, existing));
            if (removed)
            {
                removedSession = existing;
            }
        }

        if (sessions.IsEmpty)
        {
            _sessions.TryRemove(worldId, out _);
        }

        return GetUpdate(worldId, now ?? DateTimeOffset.UtcNow, removed, removedSession);
    }

    public IReadOnlyList<WorldSessionUpdate> Expire(DateTimeOffset? now = null)
    {
        var currentTime = now ?? DateTimeOffset.UtcNow;
        var updates = new List<WorldSessionUpdate>();
        foreach (var pair in _sessions)
        {
            var removedSessions = new List<WorldSessionContext>();
            foreach (var session in pair.Value)
            {
                if (IsLive(session.Value, currentTime))
                {
                    continue;
                }

                if (((ICollection<KeyValuePair<string, WorldSessionContext>>)pair.Value).Remove(session))
                {
                    removedSessions.Add(session.Value);
                }
            }

            if (pair.Value.IsEmpty)
            {
                _sessions.TryRemove(pair.Key, out _);
            }

            if (removedSessions.Count > 0)
            {
                var update = GetUpdate(pair.Key, currentTime, true);
                updates.AddRange(removedSessions.Select(removed => update with { RemovedSession = removed }));
            }
        }

        return updates;
    }

    public bool TryGetValue(int worldId, out WorldSessionContext? session)
    {
        session = GetUpdate(worldId, DateTimeOffset.UtcNow, false).ActiveSession;
        return session != null;
    }

    public WorldSessionContext? GetOrDefault(int worldId) =>
        GetUpdate(worldId, DateTimeOffset.UtcNow, false).ActiveSession;

    public bool TryGetAvailableExact(
        int worldId,
        string instanceId,
        long generation,
        out WorldSessionContext session)
    {
        var update = GetUpdate(worldId, DateTimeOffset.UtcNow, false);
        if (!update.IsAvailable ||
            update.ActiveSession is not { } activeSession ||
            activeSession.InstanceId != instanceId ||
            activeSession.Generation != generation)
        {
            session = null!;
            return false;
        }

        session = activeSession;
        return true;
    }

    public bool TryAdd(int worldId, WorldSessionContext session)
    {
        var normalized = string.IsNullOrWhiteSpace(session.InstanceId)
            ? session with { InstanceId = $"legacy-{worldId}" }
            : session;
        return ObserveOnline(normalized with { WorldId = worldId }).Changed;
    }

    public bool TryRemove(int worldId)
    {
        return _sessions.TryRemove(worldId, out _);
    }

    private WorldSessionUpdate GetUpdate(
        int worldId,
        DateTimeOffset now,
        bool changed,
        WorldSessionContext? removedSession = null)
    {
        if (!_sessions.TryGetValue(worldId, out var sessions))
        {
            return new WorldSessionUpdate(worldId, null, false, changed, removedSession);
        }

        var live = sessions.Values.Where(session => IsLive(session, now)).ToArray();
        return new WorldSessionUpdate(
            worldId,
            live.Length == 1 ? live[0] : null,
            live.Length == 1,
            changed,
            removedSession);
    }

    private static bool IsLive(WorldSessionContext session, DateTimeOffset now) =>
        session.LeaseExpiresAt == default || session.LeaseExpiresAt > now;

    private static bool IsNewer(WorldSessionContext candidate, WorldSessionContext current)
    {
        if (candidate.Generation != current.Generation)
        {
            return candidate.Generation > current.Generation;
        }

        return candidate.LeaseExpiresAt > current.LeaseExpiresAt;
    }
}
