using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
    public sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }

        public Type BuilderType { get; }
    }
}

namespace Cysharp.Threading.Tasks
{
    [AsyncMethodBuilder(typeof(CompilerServices.AsyncUniTaskMethodBuilder))]
    public struct UniTask
    {
        public Awaiter GetAwaiter() => default;
        public void Forget() { }
        public UniTask Preserve() => this;
        public static UniTask CompletedTask => default;
        public static UniTask Delay(int millisecondsDelay, bool ignoreTimeScale = false) => default;
        public static UniTask Delay(TimeSpan delay, bool ignoreTimeScale = false) => default;
        public static UniTask Yield() => default;
        public static UniTask Void(Func<UniTaskVoid> action) { action().Forget(); return default; }
        public static UniTask WhenAll(IEnumerable<UniTask> tasks) => default;
        public static UniTask WhenAll(params UniTask[] tasks) => default;

        public struct Awaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted => true;
            public void GetResult() { }
            public void OnCompleted(Action continuation) => continuation?.Invoke();
            public void UnsafeOnCompleted(Action continuation) => continuation?.Invoke();
        }
    }

    [AsyncMethodBuilder(typeof(CompilerServices.AsyncUniTaskMethodBuilder<>))]
    public struct UniTask<T>
    {
        private readonly T _result;
        public UniTask(T result) { _result = result; }
        public Awaiter GetAwaiter() => new Awaiter(_result);
        public void Forget() { }
        public UniTask Preserve() => this;
        public static implicit operator UniTask(UniTask<T> task) => default;

        public struct Awaiter : ICriticalNotifyCompletion
        {
            private readonly T _result;
            public Awaiter(T result) { _result = result; }
            public bool IsCompleted => true;
            public T GetResult() => _result;
            public void OnCompleted(Action continuation) => continuation?.Invoke();
            public void UnsafeOnCompleted(Action continuation) => continuation?.Invoke();
        }
    }

    [AsyncMethodBuilder(typeof(CompilerServices.AsyncUniTaskVoidMethodBuilder))]
    public struct UniTaskVoid
    {
        public void Forget() { }
        public Awaiter GetAwaiter() => default;

        public struct Awaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted => true;
            public void GetResult() { }
            public void OnCompleted(Action continuation) => continuation?.Invoke();
            public void UnsafeOnCompleted(Action continuation) => continuation?.Invoke();
        }
    }

    public static class UniTaskExtensions
    {
        public static UniTask AsUniTask(this System.Threading.Tasks.Task task) => default;
        public static UniTask<T> AsUniTask<T>(this System.Threading.Tasks.Task<T> task) => default;
        public static void Forget(this UniTask task) { }
        public static void Forget<T>(this UniTask<T> task) { }
        public static void Forget(this UniTaskVoid task) { }
        public static UniTask AttachExternalCancellation(this UniTask task, CancellationToken token) => task;
        public static UniTask Timeout(this UniTask task, TimeSpan timeout) => task;
    }

    public interface IProgress<T>
    {
        void Report(T value);
    }
}

namespace Cysharp.Threading.Tasks.CompilerServices
{
    public struct AsyncUniTaskMethodBuilder
    {
        public static AsyncUniTaskMethodBuilder Create() => default;
        public UniTask Task => default;
        public void SetException(Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine { }
    }

    public struct AsyncUniTaskMethodBuilder<T>
    {
        public static AsyncUniTaskMethodBuilder<T> Create() => default;
        public UniTask<T> Task => default;
        public void SetException(Exception exception) { }
        public void SetResult(T result) { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine { }
    }

    public struct AsyncUniTaskVoidMethodBuilder
    {
        public static AsyncUniTaskVoidMethodBuilder Create() => default;
        public UniTaskVoid Task => default;
        public void SetException(Exception exception) { }
        public void SetResult() { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine { }
    }
}
