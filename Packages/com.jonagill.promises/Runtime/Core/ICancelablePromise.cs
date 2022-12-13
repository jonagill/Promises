using System;

namespace Promises
{
    public interface IReadOnlyCancelablePromise : IPromise
    {
        bool IsCanceled { get; }
        IReadOnlyCancelablePromise Canceled(Action onCancel);
        new IReadOnlyCancelablePromise Then(Action onComplete);
        new IReadOnlyCancelablePromise Catch(Action<Exception> onThrow);
        new IReadOnlyCancelablePromise Finally(Action onFinish);

        // Additional IPromise Continues
        IPromise ContinueWith(
            Func<IPromise> onComplete,
            Func<IPromise> onCancel);

        IPromise ContinueWith(
            Func<IPromise> onComplete,
            Func<IPromise> onCancel,
            Func<Exception, IPromise> onThrow);

        // Additional IPromise<T> Continues
        IPromise<T> ContinueWith<T>(
            Func<IPromise<T>> onComplete, 
            Func<IPromise<T>> onCancel);

        IPromise<T> ContinueWith<T>(
            Func<IPromise<T>> onComplete, 
            Func<IPromise<T>> onCancel,
            Func<Exception, IPromise<T>> onThrow);

        new IReadOnlyCancelablePromise<T> Transform<T>(Func<T> transformResult);
        new IReadOnlyCancelablePromise TransformException(Func<Exception, Exception> transformException);
    }
    
    public interface ICancelablePromise : IReadOnlyCancelablePromise
    {
        void CancelIfPending();
        void Cancel();
        
        new ICancelablePromise Canceled(Action onCancel);
        new ICancelablePromise Then(Action onComplete);
        new ICancelablePromise Catch(Action<Exception> onThrow);
        new ICancelablePromise Finally(Action onFinish);
        
        // ICancelablePromise Continues
        ICancelablePromise ContinueWith(
            Func<ICancelablePromise> onComplete);

        ICancelablePromise ContinueWith(
            Func<ICancelablePromise> onComplete,
            Func<Exception, ICancelablePromise> onThrow);

        // ICancelablePromise<T> Continues
        ICancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> onComplete);

        ICancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> onComplete,
            Func<Exception, ICancelablePromise<T>> onThrow);

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
        
        // Additional IPromise Continues
        IPromise ContinueWith(Func<T, IPromise> onComplete, Func<IPromise> onCancel);
        IPromise ContinueWith(Func<T, IPromise> onComplete, Func<IPromise> onCancel, Func<Exception, IPromise> onThrow);
        
        // Additional IPromise<U> Continues
        IPromise<U> ContinueWith<U>(Func<T, IPromise<U>> onComplete, Func<IPromise<U>> onCancel);
        IPromise<U> ContinueWith<U>(Func<T, IPromise<U>> onComplete, Func<IPromise<U>> onCancel, Func<Exception, IPromise<U>> onThrow);

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
        
        // ICancelablePromise Continues
        ICancelablePromise ContinueWith(
            Func<T, ICancelablePromise> onComplete);

        ICancelablePromise ContinueWith(
            Func<T, ICancelablePromise> onComplete,
            Func<Exception, ICancelablePromise> onThrow);
        

        // ICancelablePromise<U> Continues
        ICancelablePromise<U> ContinueWith<U>(
            Func<T, ICancelablePromise<U>> onComplete);

        ICancelablePromise<U> ContinueWith<U>(
            Func<T, ICancelablePromise<U>> onComplete,
            Func<Exception, ICancelablePromise<U>> onThrow);

        new ICancelablePromise<U> Transform<U>(Func<T, U> transformResult);
        new ICancelablePromise<T> TransformException(Func<Exception, Exception> transformException);
    }
}

