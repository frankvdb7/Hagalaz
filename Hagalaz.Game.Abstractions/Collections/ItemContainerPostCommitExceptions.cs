using System;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Reports a completion-effects failure after item-container storage became irreversible.</summary>
/// <remarks>
/// When thrown by <see cref="ItemContainerTransaction.Commit"/>, storage remains committed and cannot be rolled
/// back. <see cref="Exception.InnerException"/> is the exact original callback exception.
/// </remarks>
public sealed class ItemContainerPostCommitCompletionException : Exception
{
    /// <summary>Initializes an exception for a failed transaction-owned completion-effects callback.</summary>
    /// <param name="innerException">The original exception thrown by the callback.</param>
    public ItemContainerPostCommitCompletionException(Exception innerException)
        : base("Item-container storage committed, but completion effects failed.",
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
    }
}

/// <summary>Reports a publication failure after item-container storage became irreversible.</summary>
/// <remarks>
/// When thrown by <see cref="ItemContainerTransaction.Commit"/>, storage remains committed and cannot be rolled
/// back. <see cref="Exception.InnerException"/> is the exact original callback exception.
/// </remarks>
public sealed class ItemContainerPostCommitPublicationException : Exception
{
    /// <summary>Initializes an exception for a failed transaction-owned publication callback.</summary>
    /// <param name="innerException">The original exception thrown by the callback.</param>
    public ItemContainerPostCommitPublicationException(Exception innerException)
        : base("Item-container storage committed, but publication failed.",
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
    }
}
