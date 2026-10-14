using System;
using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Represents one item-container write operation.</summary>
internal ref struct ItemContainerWriteScope
{
    private readonly ItemContainerSynchronization _synchronization;
    private readonly ItemContainerTransaction? _transaction;
    private readonly int _originatingThreadId;
    private readonly bool _ownsLock;
    private bool _recorded;
    private bool _disposed;

    internal ItemContainerTransaction? Transaction => _transaction;

    internal ItemContainerWriteScope(ItemContainerSynchronization synchronization, bool ownsLock,
        ItemContainerTransaction? transaction, int originatingThreadId)
    {
        _synchronization = synchronization;
        _transaction = transaction;
        _originatingThreadId = originatingThreadId;
        _ownsLock = ownsLock;
        _recorded = false;
        _disposed = false;
    }

    /// <summary>Records changed slots. A <see langword="null"/> set means the whole container changed.</summary>
    internal void RecordChanges(HashSet<int>? slots)
    {
        EnsureCurrentThread();
        if (_disposed) throw new ObjectDisposedException(nameof(ItemContainerWriteScope));
        if (_recorded) throw new InvalidOperationException("Write changes can only be recorded once per operation.");

        _synchronization.RecordChanges(_transaction, slots);
        _recorded = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        EnsureCurrentThread();
        _disposed = true;

        var completionOwner = _synchronization.EndWrite(_ownsLock, _transaction);
        if (completionOwner == null) return;

        try
        {
            completionOwner.CompleteCommittedEffects();
            completionOwner.PublishCommittedChanges();
        }
        finally
        {
            _synchronization.CompleteStandalonePublication();
        }
    }

    private void EnsureCurrentThread()
    {
        if (_originatingThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("Write scopes must be used and disposed on their originating thread.");
    }
}
