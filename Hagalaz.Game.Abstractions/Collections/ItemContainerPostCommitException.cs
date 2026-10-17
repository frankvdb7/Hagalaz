using System;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Identifies which transaction-owned completion phase failed after item storage was committed.</summary>
public enum ItemContainerPostCommitPhase
{
    /// <summary>A domain completion-effects callback failed.</summary>
    Completion,

    /// <summary>A domain change-publication callback failed.</summary>
    Publication
}

/// <summary>Reports a transaction completion failure after item-container storage became irreversible.</summary>
/// <remarks>
/// When this exception is thrown by <see cref="ItemContainerTransaction.Commit"/>, storage remains committed and
/// cannot be rolled back. <see cref="Exception.InnerException"/> is the original callback exception.
/// </remarks>
public sealed class ItemContainerPostCommitException : Exception
{
    /// <summary>Initializes an exception for a failed transaction-owned completion phase.</summary>
    /// <param name="phase">The completion phase that failed.</param>
    /// <param name="innerException">The original exception thrown by the callback.</param>
    public ItemContainerPostCommitException(ItemContainerPostCommitPhase phase, Exception innerException)
        : base(CreateMessage(phase), innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
        Phase = phase;
    }

    /// <summary>Gets the transaction-owned completion phase that failed.</summary>
    public ItemContainerPostCommitPhase Phase { get; }

    private static string CreateMessage(ItemContainerPostCommitPhase phase) => phase switch
    {
        ItemContainerPostCommitPhase.Completion => "Item-container storage committed, but completion failed.",
        ItemContainerPostCommitPhase.Publication => "Item-container storage committed, but publication failed.",
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown post-commit phase.")
    };
}
