using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    public class RsTaskHandle<TResult> : RsTaskHandle, IRsTaskHandle<TResult>
    {
        private readonly ITaskItem<TResult> _taskItemT;

        public RsTaskHandle(ITaskItem<TResult> taskItem) : base(taskItem) => _taskItemT = taskItem;

        public void RegisterResultHandler(Action<TResult> resultHandler) => _taskItemT.RegisterResultHandler(resultHandler);
    }

    public class RsTaskHandle : IRsTaskHandle
    {
        private readonly ITaskItem _taskItem;

        public RsTaskHandle(ITaskItem taskItem) => _taskItem = taskItem;

        public void Cancel() => _taskItem.Cancel();
    }
}