using System;

namespace Hagalaz.Game.Abstractions.Collections;

/// <summary>Reports a failure surfaced during the completion-effects phase after item-container storage became irreversible.</summary>
/// <remarks>
/// When thrown by <see cref="ItemContainerTransaction.Commit"/>, storage remains committed and cannot be rolled
/// back. The exception type identifies the phase in which an owner reported the failure. <see cref="Exception.InnerException"/>
/// is the exact exception reported by the owner.
/// </remarks>
public sealed class ItemContainerPostCommitCompletionException : Exception
{
    /// <summary>Initializes an exception for a failure reported during transaction-owned completion effects.</summary>
    /// <param name="innerException">The exception reported during completion effects.</param>
    public ItemContainerPostCommitCompletionException(Exception innerException)
        : base("Item-container storage committed, but completion effects failed.",
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
    }
}

/// <summary>Reports a failure surfaced during the publication phase after item-container storage became irreversible.</summary>
/// <remarks>
/// When thrown by <see cref="ItemContainerTransaction.Commit"/>, storage remains committed and cannot be rolled
/// back. The exception type identifies the phase in which an owner reported the failure, not necessarily its origin.
/// For example, Equipment reports retained lifecycle-hook failures only after attempting its state publication.
/// <see cref="Exception.InnerException"/> is the exact exception reported by the owner.
/// </remarks>
public sealed class ItemContainerPostCommitPublicationException : Exception
{
    /// <summary>Initializes an exception for a failure reported during transaction-owned publication.</summary>
    /// <param name="innerException">The exception reported by the owner during publication.</param>
    public ItemContainerPostCommitPublicationException(Exception innerException)
        : base("Item-container storage committed, but the publication phase reported a failure.",
            innerException ?? throw new ArgumentNullException(nameof(innerException)))
    {
    }
}
