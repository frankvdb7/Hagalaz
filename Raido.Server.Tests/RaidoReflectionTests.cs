using System.Reflection;
using System.Runtime.CompilerServices;
using Raido.Server.Internal.Reflection;

namespace Raido.Server.Tests;

[TestClass]
public sealed class RaidoReflectionTests
{
    private class ReflectionTarget
    {
        public int Value { get; private set; }
        public void SetValue(int value) => Value = value;
        public int Add(int value) => Value + value;
        public Task RunAsync() => Task.CompletedTask;
        public async Task<int> GetAsync(int value) => await Task.FromResult(value + 1);
        public ValueTask<int> GetValueTask(int value) => ValueTask.FromResult(value + 2);
        public string NotAwaitable() => "not awaitable";
        public virtual void InheritedBaseMethod() { }
    }

    private sealed class DerivedReflectionTarget : ReflectionTarget
    {
        public void OwnMethod() { }
    }

    private sealed class NoAwaiter
    {
        public bool IsCompleted => true;
    }

    private sealed class NoCompleted
    {
        public NoCompletedAwaiter GetAwaiter() => new();
    }

    private sealed class NoCompletedAwaiter : INotifyCompletion
    {
        public void OnCompleted(Action continuation) => continuation();
        public int GetResult() => 1;
    }

    private sealed class NoNotify
    {
        public NoNotifyAwaiter GetAwaiter() => new();
    }

    private sealed class NoNotifyAwaiter
    {
        public bool IsCompleted => true;
        public int GetResult() => 1;
    }

    private sealed class NoResult
    {
        public NoResultAwaiter GetAwaiter() => new();
    }

    private sealed class NoResultAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public void OnCompleted(Action continuation) => continuation();
    }

    private sealed class NotifyOnlyAwaitable
    {
        public NotifyOnlyAwaiter GetAwaiter() => new();
    }

    private sealed class NotifyOnlyAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public void OnCompleted(Action continuation) => continuation();
        public void GetResult() { }
    }

    [TestMethod]
    public void AwaitableInfo_RecognizesTaskValueTaskAndRejectsInvalidPatterns()
    {
        Assert.IsTrue(AwaitableInfo.IsTypeAwaitable(typeof(Task<int>), out var taskInfo));
        Assert.AreEqual(typeof(int), taskInfo.ResultType);
        Assert.IsNotNull(taskInfo.AwaiterUnsafeOnCompletedMethod);
        Assert.IsTrue(AwaitableInfo.IsTypeAwaitable(typeof(NotifyOnlyAwaitable), out var notifyOnlyInfo));
        Assert.AreEqual(typeof(void), notifyOnlyInfo.ResultType);
        Assert.IsNull(notifyOnlyInfo.AwaiterUnsafeOnCompletedMethod);
        Assert.IsTrue(AwaitableInfo.IsTypeAwaitable(typeof(ValueTask), out _));
        Assert.IsFalse(AwaitableInfo.IsTypeAwaitable(typeof(int), out _));
        Assert.IsFalse(AwaitableInfo.IsTypeAwaitable(typeof(NoAwaiter), out _));
        Assert.IsFalse(AwaitableInfo.IsTypeAwaitable(typeof(NoCompleted), out _));
        Assert.IsFalse(AwaitableInfo.IsTypeAwaitable(typeof(NoNotify), out _));
        Assert.IsFalse(AwaitableInfo.IsTypeAwaitable(typeof(NoResult), out _));
    }

    [TestMethod]
    public async Task ObjectMethodExecutor_InvokesSyncAndAsyncMethods()
    {
        var target = new ReflectionTarget();
        var set = ObjectMethodExecutor.Create(typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.SetValue))!, typeof(ReflectionTarget).GetTypeInfo());
        Assert.IsFalse(set.IsMethodAsync);
        Assert.IsNull(set.Execute(target, new object[] { 7 }));
        Assert.AreEqual(7, target.Value);

        var add = ObjectMethodExecutor.Create(typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.Add))!, typeof(ReflectionTarget).GetTypeInfo());
        Assert.AreEqual(10, add.Execute(target, new object[] { 3 }));

        var run = ObjectMethodExecutor.Create(typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.RunAsync))!, typeof(ReflectionTarget).GetTypeInfo());
        Assert.IsTrue(run.IsMethodAsync);
        await run.ExecuteAsync(target, Array.Empty<object>());
        var get = ObjectMethodExecutor.Create(typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.GetAsync))!, typeof(ReflectionTarget).GetTypeInfo());
        Assert.AreEqual(5, await get.ExecuteAsync(target, new object[] { 4 }));
        var valueTask = ObjectMethodExecutor.Create(typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.GetValueTask))!, typeof(ReflectionTarget).GetTypeInfo());
        Assert.AreEqual(6, await valueTask.ExecuteAsync(target, new object[] { 4 }));
    }

    [TestMethod]
    public void ObjectMethodExecutor_ValidatesDefaultsAndNonAsyncMethods()
    {
        var method = typeof(ReflectionTarget).GetMethod(nameof(ReflectionTarget.Add))!;
        var executor = ObjectMethodExecutor.Create(method, typeof(ReflectionTarget).GetTypeInfo(), new object[] { 10 });
        Assert.AreEqual(10, executor.GetDefaultValueForParameter(0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => executor.GetDefaultValueForParameter(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => executor.GetDefaultValueForParameter(1));
        var withoutDefaults = ObjectMethodExecutor.Create(method, typeof(ReflectionTarget).GetTypeInfo());
        Assert.ThrowsExactly<InvalidOperationException>(() => withoutDefaults.GetDefaultValueForParameter(0));
        Assert.ThrowsExactly<ArgumentNullException>(() => ObjectMethodExecutor.Create(null!, typeof(ReflectionTarget).GetTypeInfo()));
        Assert.ThrowsExactly<ArgumentNullException>(() => ObjectMethodExecutor.Create(method, typeof(ReflectionTarget).GetTypeInfo(), null!));
        Assert.IsFalse(CoercedAwaitableInfo.IsTypeAwaitable(typeof(string), out _));
        Assert.IsTrue(CoercedAwaitableInfo.IsTypeAwaitable(typeof(Task), out var taskInfo));
        Assert.IsFalse(taskInfo.RequiresCoercion);
    }

    [TestMethod]
    public void ObjectMethodExecutorAwaitable_DelegatesAwaiterOperations()
    {
        var continued = false;
        var unsafeContinued = false;
        var awaitable = new ObjectMethodExecutorAwaitable(
            new object(),
            _ => new object(),
            _ => false,
            _ => 42,
            (_, callback) => { continued = true; callback(); },
            (_, callback) => { unsafeContinued = true; callback(); });
        var awaiter = awaitable.GetAwaiter();
        Assert.IsFalse(awaiter.IsCompleted);
        Assert.AreEqual(42, awaiter.GetResult());
        awaiter.OnCompleted(() => { });
        awaiter.UnsafeOnCompleted(() => { });
        Assert.IsTrue(continued);
        Assert.IsTrue(unsafeContinued);

        var fallback = new ObjectMethodExecutorAwaitable(new object(), _ => new object(), _ => true, _ => null!, (_, _) => { }, null).GetAwaiter();
        fallback.UnsafeOnCompleted(() => { });
    }

    [TestMethod]
    public void HubReflectionHelper_ExcludesObjectAndDisposeMethods()
    {
        var methods = RaidoHubReflectionHelper.GetHubMethods(typeof(DerivedReflectionTarget)).Select(x => x.Name).ToArray();
        CollectionAssert.Contains(methods, nameof(DerivedReflectionTarget.OwnMethod));
        CollectionAssert.DoesNotContain(methods, nameof(object.ToString));
        CollectionAssert.DoesNotContain(methods, nameof(IDisposable.Dispose));
        CollectionAssert.DoesNotContain(methods, nameof(RaidoHub.OnConnectedAsync));
    }
}
