using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    public interface IRsTaskHandle<out TResult> : IRsTaskHandle
    {
        void RegisterResultHandler(Action<TResult> resultHandler);
    }

    public interface IRsTaskHandle
    {
        void Cancel();
    }
}