using System;

namespace Promises 
{
    public interface IPromise
    {
        bool IsPending { get; }
        bool HasSucceeded { get; }
        bool HasException { get; }

        IPromise Then(Action onComplete);
        IPromise Catch(Exception onThrow);
        IPromise Finally(Action onFinish);
        
        IPromise ContinueWith(Func<IPromise> transformResult, Func<Exception, IPromise> transformException = null);
        IPromise<T> ContinueWith<T>(Func<IPromise<T>> transformResult, Func<Exception, IPromise<T>> transformException = null);
        IPromise<T> Transform<T>(Func<T> transformResult);
        IPromise TransformException(Func<Exception, Exception> transformResult);
    }

    public interface IPromise<T> : IPromise
    {
        IPromise<T> Then(Action<T> onComplete);
        new IPromise<T> Then(Action onComplete);
        new IPromise<T> Exception(Action<Exception> onThrow);
        new IPromise<T> Finally(Action onFinish);
        
        IPromise<U> ContinueWith<U>(Func<T, IPromise<U>> transformResult, Func<string, IPromise<U>> transformException = null);
        IPromise ContinueWith(Func<T, IPromise> transformResult, Func<Exception, IPromise> transformException = null);
        IPromise<U> Transform<U>(Func<T, U> transformResult);
        new IPromise<T> TransformException(Func<Exception, Exception> transformException);
    }
}

