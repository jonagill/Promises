using System;
using System.Collections;

namespace Promises 
{
    public interface IPromise : IEnumerator
    {
        bool IsPending { get; }
        bool HasSucceeded { get; }
        bool HasException { get; }

        IPromise Then(Action onComplete);
        IPromise Catch(Action<Exception> onThrow);
        IPromise Finally(Action onFinish);
        
        IPromise ContinueWith(Func<IPromise> onComplete, Func<Exception, IPromise> onThrow = null);
        IPromise<T> ContinueWith<T>(Func<IPromise<T>> onComplete, Func<Exception, IPromise<T>> onThrow = null);
        
        IPromise<T> Transform<T>(Func<T> transformResult);
        IPromise TransformException(Func<Exception, Exception> transformException);
    }

    public interface IPromise<T> : IPromise
    {
        IPromise<T> Then(Action<T> onComplete);
        new IPromise<T> Then(Action onComplete);
        new IPromise<T> Catch(Action<Exception> onThrow);
        new IPromise<T> Finally(Action onFinish);

        IPromise ContinueWith(Func<T, IPromise> onComplete, Func<Exception, IPromise> onThrow = null);
        IPromise<U> ContinueWith<U>(Func<T, IPromise<U>> onComplete, Func<Exception, IPromise<U>> onThrow = null);
        
        IPromise<U> Transform<U>(Func<T, U> transformResult);
        new IPromise<T> TransformException(Func<Exception, Exception> transformException);
    }
}

