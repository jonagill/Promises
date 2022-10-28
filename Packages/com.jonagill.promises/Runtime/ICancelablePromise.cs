using System;
using System.Threading;

namespace Promises
{
    public interface IReadOnlyCancelablePromise : IPromise
    {
        bool IsCanceled { get; }

        ICancelablePromise Canceled(Action onCancel);
        
        ICancelablePromise ContinueWith(Func<ICancelablePromise> transformResult, Func<Exception, IPromise> transformException = null);
        ICancelablePromise<T> ContinueWith<T>(Func<ICancelablePromise> transformResult, Func<Exception, IPromise<T>> transformException = null);
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
        
        ICancelablePromise<U> ContinueWith<U>(Func<T, ICancelablePromise<U>> transformResult, Func<string, ICancelablePromise<U>> transformException = null);
        ICancelablePromise ContinueWith(Func<T, ICancelablePromise> transformResult, Func<Exception, ICancelablePromise> transformException = null);
        ICancelablePromise<U> Transform<U>(Func<T, U> transformResult);
        ICancelablePromise<T> TransformException(Func<Exception, Exception> transformException);
    }

    public interface ICancelablePromise<T> : IPromise<T>, IReadOnlyCancelablePromise<T>, ICancelablePromise { }
}

