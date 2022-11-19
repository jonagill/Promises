using System;
using System.Threading;

namespace Promises
{
    public enum CancellationChainingType
    {
        CancelAll,
        CancelChildren,
        DontChain,
    }
    
    public interface IReadOnlyCancelablePromise : IPromise
    {
        bool IsCanceled { get; }

        ICancelablePromise Canceled(Action onCancel);
        
        new ICancelablePromise Then(Action onComplete);
        new ICancelablePromise Catch(Action<Exception> onThrow);
        new ICancelablePromise Finally(Action onFinish);
        
        ICancelablePromise ContinueWith(
            Func<ICancelablePromise> transformResult, 
            Func<ICancelablePromise> transformCanceled = null,
            Func<Exception, ICancelablePromise> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        ICancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> transformResult,
            Func<ICancelablePromise> transformCanceled = null,
            Func<Exception, ICancelablePromise<T>> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        
        new ICancelablePromise<T> Transform<T>(Func<T> transformResult);
        new ICancelablePromise TransformException(Func<Exception, Exception> transformResult);
    }
    
    public interface ICancelablePromise : IReadOnlyCancelablePromise
    {
        CancellationToken CancellationToken { get; }
        void Cancel();
    }
    
    public interface IReadOnlyCancelablePromise<T> : IPromise<T>, IReadOnlyCancelablePromise
    {
        new ICancelablePromise<T> Canceled(Action onCancel);
        
        new ICancelablePromise Then(Action<T> onComplete);
        new ICancelablePromise<T> Then(Action onComplete);
        new ICancelablePromise<T> Catch(Action<Exception> onThrow);
        new ICancelablePromise<T> Finally(Action onFinish);
        
        ICancelablePromise<U> ContinueWith<U>(
            Func<T, ICancelablePromise<U>> transformResult,
            Func<ICancelablePromise> transformCanceled,
            Func<Exception, ICancelablePromise<U>> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        ICancelablePromise ContinueWith(
            Func<T, ICancelablePromise> transformResult,
            Func<ICancelablePromise> transformCanceled,
            Func<Exception, ICancelablePromise> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        ICancelablePromise<U> Transform<U>(Func<T, U> transformResult);
        ICancelablePromise<T> TransformException(Func<Exception, Exception> transformException);
    }

    public interface ICancelablePromise<T> : IPromise<T>, IReadOnlyCancelablePromise<T>, ICancelablePromise { }
}

