using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Raido.Server;

/// <summary>
/// The stable application-facing duplex pipe for one logical Raido connection.
/// </summary>
internal sealed class RaidoApplicationConnection : IDuplexPipe
{
    private readonly Pipe _input = new();
    private readonly Pipe _output = new();
    private readonly TaskCompletionSource<RaidoApplicationExitReason> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public PipeReader Input => _input.Reader;

    public PipeWriter Output => _output.Writer;

    internal PipeWriter InputWriter => _input.Writer;

    internal PipeReader OutputReader => _output.Reader;

    internal Task<RaidoApplicationExitReason> Completion => _completion.Task;

    internal void Complete(RaidoApplicationExitReason reason, Exception? error = null)
    {
        if (_completion.TrySetResult(reason))
        {
            try { _input.Writer.Complete(error); } catch (InvalidOperationException) { }
            try { _output.Reader.Complete(error); } catch (InvalidOperationException) { }
            try { _output.Writer.Complete(error); } catch (InvalidOperationException) { }
        }
    }
}

internal enum RaidoApplicationExitReason
{
    Terminal,
    Transferred
}
