using System;
using System.Threading;

namespace Promises
{
    public enum CancellationChainingType
    {
        /// <summary>
        /// Chains two CancelablePromises using the same CancellationTokenSource,
        /// so that cancelling one cancels both the promise it is chaining from
        /// and any promises that chain from it.
        /// </summary>
        CancelAll,
        /// <summary>
        /// Chains two CancelablePromises using the same CancellationToken,
        /// so that canceling the original promise cancels the promises that
        /// chain from it.
        /// </summary>
        CancelChildren,
        /// <summary>
        /// Chains two CancelablePromises with completely unrelated CancellationTokens,
        /// so that canceling either promise does not cancel the other.
        /// </summary>
        DontChain,
    }
    
    public interface IReadOnlyCancelablePromise : IPromise
    {
        bool IsCanceled { get; }
        IReadOnlyCancelablePromise Canceled(Action onCancel);
        new IReadOnlyCancelablePromise Then(Action onComplete);
        new IReadOnlyCancelablePromise Catch(Action<Exception> onThrow);
        new IReadOnlyCancelablePromise Finally(Action onFinish);
        
        IReadOnlyCancelablePromise ContinueWith(
            Func<ICancelablePromise> onComplete, 
            Func<ICancelablePromise> onCancel = null,
            Func<Exception, ICancelablePromise> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        IReadOnlyCancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> onComplete,
            Func<ICancelablePromise<T>> onCancel = null,
            Func<Exception, ICancelablePromise<T>> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        
        new IReadOnlyCancelablePromise<T> Transform<T>(Func<T> transformResult);
        new IReadOnlyCancelablePromise TransformException(Func<Exception, Exception> transformException);
    }
    
    public interface ICancelablePromise : IReadOnlyCancelablePromise
    {
        void Cancel();
        
        new ICancelablePromise Canceled(Action onCancel);
        new ICancelablePromise Then(Action onComplete);
        new ICancelablePromise Catch(Action<Exception> onThrow);
        new ICancelablePromise Finally(Action onFinish);
        
        new ICancelablePromise ContinueWith(
            Func<ICancelablePromise> onComplete, 
            Func<ICancelablePromise> onCancel = null,
            Func<Exception, ICancelablePromise> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        new ICancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> onComplete,
            Func<ICancelablePromise<T>> onCancel = null,
            Func<Exception, ICancelablePromise<T>> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        
        new ICancelablePromise<T> Transform<T>(Func<T> transformResult);
        new ICancelablePromise TransformException(Func<Exception, Exception> transformException);
    }
    
    public interface IReadOnlyCancelablePromise<T> : IPromise<T>, IReadOnlyCancelablePromise
    {
        new IReadOnlyCancelablePromise<T> Canceled(Action onCancel);
        new IReadOnlyCancelablePromise Then(Action<T> onComplete);
        new IReadOnlyCancelablePromise<T> Then(Action onComplete);
        new IReadOnlyCancelablePromise<T> Catch(Action<Exception> onThrow);
        new IReadOnlyCancelablePromise<T> Finally(Action onFinish);
        
        IReadOnlyCancelablePromise<U> ContinueWith<U>(
            Func<T, ICancelablePromise<U>> onComplete,
            Func<ICancelablePromise<U>> onCancel,
            Func<Exception, ICancelablePromise<U>> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        IReadOnlyCancelablePromise ContinueWith(
            Func<T, ICancelablePromise> onComplete,
            Func<ICancelablePromise> onCancel,
            Func<Exception, ICancelablePromise> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        new IReadOnlyCancelablePromise<U> Transform<U>(Func<T, U> transformResult);
        new IReadOnlyCancelablePromise<T> TransformException(Func<Exception, Exception> transformException);
    }

    public interface ICancelablePromise<T> : IPromise<T>, IReadOnlyCancelablePromise<T>, ICancelablePromise
    {
        new ICancelablePromise<T> Canceled(Action onCancel);
        
        new ICancelablePromise Then(Action<T> onComplete);
        new ICancelablePromise<T> Then(Action onComplete);
        new ICancelablePromise<T> Catch(Action<Exception> onThrow);
        new ICancelablePromise<T> Finally(Action onFinish);
        
        new ICancelablePromise<U> ContinueWith<U>(
            Func<T, ICancelablePromise<U>> onComplete,
            Func<ICancelablePromise<U>> onCancel,
            Func<Exception, ICancelablePromise<U>> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        new ICancelablePromise ContinueWith(
            Func<T, ICancelablePromise> onComplete,
            Func<ICancelablePromise> onCancel,
            Func<Exception, ICancelablePromise> onThrow = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll);
        new ICancelablePromise<U> Transform<U>(Func<T, U> transformResult);
        new ICancelablePromise<T> TransformException(Func<Exception, Exception> transformException);
    }
}

