using System;
using System.Collections;
using System.Collections.Generic;

namespace Promises
{
    public class Promise : IPromise
    {
        #region Static Helpers

        /// <summary>
        /// An already completed promise
        /// Use when you need to return something to indicate that an operation has already completed.
        /// </summary>
        public static readonly IPromise CompletedPromise;

        static Promise()
        {
            var promise = new Promise();
            promise.Complete();
            CompletedPromise = promise;
        }

        /// <summary>
        /// Creates and completes an <see cref="IPromise{T}"/> with the given <paramref name="result"/>.
        /// </summary>
        public static IPromise<T> FromResult<T>(T result)
        {
            Promise<T> promise = new Promise<T>();
            promise.Complete(result);
            return promise;
        }

        /// <summary>
        /// Creates an <see cref="IPromise"/> that has already thrown the given <paramref name="exception"/>.
        /// </summary>
        public static IPromise FromException(Exception e)
        {
            Promise promise = new Promise();
            promise.Throw(e);
            return promise;
        }

        /// <summary>
        /// Creates an <see cref="IPromise{T}"/> that has already thrown the given <paramref name="exception"/>.
        /// </summary>
        public static IPromise<T> FromException<T>(Exception e)
        {
            Promise<T> promise = new Promise<T>();
            promise.Throw(e);
            return promise;
        }

        public static IPromise All(
            IPromise a, 
            IPromise b) => new AllPromise(a, b);
        
        public static IPromise All(
            IPromise a, 
            IPromise b,
            IPromise c) => new AllPromise(a, b, c);
        
        public static IPromise All(
            IPromise a, 
            IPromise b,
            IPromise c,
            IPromise d) => new AllPromise(a, b, c, d);
        
        public static IPromise All(
            IPromise a, 
            IPromise b,
            IPromise c,
            IPromise d,
            IPromise e) => new AllPromise(a, b, c, d, e);
        
        public static IPromise All(
            IPromise a, 
            IPromise b,
            IPromise c,
            IPromise d,
            IPromise e,
            IPromise f) => new AllPromise(a, b, c, d, e, f);

        public static IPromise All(params IPromise[] promises) => new AllPromise(promises);
        
        #endregion

        private readonly List<Action> _thenCallbacks = new List<Action>(1);
        private readonly List<Action<Exception>> _catchCallbacks = new List<Action<Exception>>();
        private readonly List<Action> _finallyCallbacks = new List<Action>();

        private Exception _exception = null;

        #region IPromise API

        public bool IsPending { get; protected set; } = true;
        public bool HasSucceeded => !IsPending && _exception == null;
        public bool HasException => !IsPending && _exception != null;

        public IPromise Then(Action onComplete)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            if (IsPending)
            {
                _thenCallbacks.Add(onComplete);
            }
            else if (_exception == null)
            {
                onComplete();
            }

            return this;
        }

        public IPromise Catch(Action<Exception> onThrow)
        {
            if (onThrow == null)
            {
                throw new ArgumentNullException(nameof(onThrow));
            }

            if (IsPending)
            {
                _catchCallbacks.Add(onThrow);
            }
            else if (_exception != null)
            {
                onThrow(_exception);
            }

            return this;
        }

        public IPromise Finally(Action onFinish)
        {
            if (onFinish == null)
            {
                throw new ArgumentNullException(nameof(onFinish));
            }

            if (IsPending)
            {
                _finallyCallbacks.Add(onFinish);
            }
            else
            {
                onFinish();
            }

            return this;
        }

        public void Complete()
        {
            if (!IsPending)
            {
                throw new InvalidOperationException($"Cannot complete a non-pending promise.");
            }

            IsPending = false;

            Exception thrownException = null;
            foreach (var callback in _thenCallbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    thrownException = e;
                }
            }
            
            ExecuteFinallyCallbacks(ref thrownException);
            ClearCallbacks();

            if (thrownException != null)
            {
                throw new PromiseExecutionException(thrownException);
            }
        }

        public void Throw(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (!IsPending)
            {
                throw new InvalidOperationException($"Cannot complete a non-pending promise.");
            }

            IsPending = false;

            Exception thrownException = null;
            foreach (var callback in _catchCallbacks)
            {
                try
                {
                    callback(exception);
                }
                catch (Exception e)
                {
                    thrownException = e;
                }
            }
            
            ExecuteFinallyCallbacks(ref thrownException);
            ClearCallbacks();

            if (thrownException != null)
            {
                throw new PromiseExecutionException(exception);
            }
        }

        #endregion

        #region Transformation Methods

        public IPromise ContinueWith(
            Func<IPromise> transformResult,
            Func<Exception, IPromise> transformException = null)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }

            var newPromise = new Promise();

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                transformResult()
                    .Catch(e => newPromise.Throw(e))
                    .Then(() => newPromise.Complete());
            });

            return newPromise;
        }

        public IPromise<T> ContinueWith<T>(
            Func<IPromise<T>> transformResult,
            Func<Exception, IPromise<T>> transformException = null)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }

            var newPromise = new Promise<T>();

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                transformResult()
                    .Catch(e => newPromise.Throw(e))
                    .Then(result => newPromise.Complete(result));
            });

            return newPromise;
        }

        public IPromise<T> Transform<T>(Func<T> transformResult)
        {
            var newPromise = new Promise<T>();
            Catch(e => newPromise.Throw(e));
            Then(() => newPromise.Complete(transformResult()));
            return newPromise;
        }

        public IPromise TransformException(Func<Exception, Exception> transformException)
        {
            var newPromise = new Promise();
            Catch(e => newPromise.Throw(transformException(e)));
            Then(() => newPromise.Complete());
            return newPromise;
        }

        #endregion

        #region IEnumerator
        
        object IEnumerator.Current => null;

        // Allow coroutines to yield on the promise until it completes
        bool IEnumerator.MoveNext()
        {
            return IsPending;
        }

        void IEnumerator.Reset() { }
        
        #endregion
        
        #region Internal Helpers

        protected void ExecuteFinallyCallbacks(ref Exception thrownException)
        {
            foreach (var callback in _finallyCallbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    thrownException = e;
                }
            }
        }
        
        protected virtual void ClearCallbacks()
        {
            // Clear all the subscribed callbacks free that memory
            // and make sure we don't hold onto any subscriber references
            // that might prevent GC from running
            _thenCallbacks.Clear();
            _catchCallbacks.Clear();
            _finallyCallbacks.Clear();
        }
        
        #endregion
    }

    public class Promise<T> : IPromise<T>
    {
        /// <summary>
        /// Custom type for .Then() callbacks so that we can invoke all callbacks
        /// in the order they were subscribed, regardless of whether they are
        /// typed or untyped callbacks.
        /// </summary>
        private struct ThenCallback
        {
            public readonly Action<T> typedCallback;
            public readonly Action untypedCallback;
            
            public ThenCallback(Action<T> callback)
            {
                typedCallback = callback;
                untypedCallback = null;
            }

            public ThenCallback(Action callback)
            {
                typedCallback = null;
                untypedCallback = callback;
            }

            public void Invoke(T result)
            {
                typedCallback?.Invoke(result);
                untypedCallback?.Invoke();
            }
        }
        
        private readonly List<ThenCallback> _thenCallbacks = new List<ThenCallback>(1);
        private readonly List<Action<Exception>> _catchCallbacks = new List<Action<Exception>>();
        private readonly List<Action> _finallyCallbacks = new List<Action>();

        private T _result;
        private Exception _exception = null;
        
        #region Promise API

        public bool IsPending { get; protected set; } = true;
        public bool HasSucceeded => !IsPending && _exception == null;
        public bool HasException => !IsPending && _exception != null;

        IPromise IPromise.Then(Action onComplete)
        {
            return Then(onComplete);
        }
        
        public IPromise<T> Then(Action onComplete)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            if (IsPending)
            {
                _thenCallbacks.Add(new ThenCallback(onComplete));
            }
            else if (_exception == null)
            {
                onComplete();
            }

            return this;
        }
        
        public IPromise<T> Then(Action<T> onComplete)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            if (IsPending)
            {
                _thenCallbacks.Add(new ThenCallback(onComplete));
            }
            else if (_exception == null)
            {
                onComplete(_result);
            }

            return this;
        }
        
        IPromise IPromise.Catch(Action<Exception> onThrow)
        {
            return Catch(onThrow);
        }
        
        public IPromise<T> Catch(Action<Exception> onThrow)
        {
            if (onThrow == null)
            {
                throw new ArgumentNullException(nameof(onThrow));
            }
            
            if (IsPending)
            {
                _catchCallbacks.Add(onThrow);
            }
            else if (_exception != null)
            {
                onThrow(_exception);
            }

            return this;
        }
        
        IPromise IPromise.Finally(Action onFinish)
        {
            return Finally(onFinish);
        }

        public virtual IPromise<T> Finally(Action onFinish)
        {
            if (onFinish == null)
            {
                throw new ArgumentNullException(nameof(onFinish));
            }
            
            if (IsPending)
            {
                _finallyCallbacks.Add(onFinish);
            }
            else
            {
                onFinish();
            }

            return this;
        }

        public void Complete(T result)
        {
            if (!IsPending)
            {
                throw new InvalidOperationException($"Cannot complete a non-pending promise.");
            }

            _result = result;
            IsPending = false;

            Exception thrownException = null;
            foreach (var callback in _thenCallbacks)
            {
                try
                {
                    callback.Invoke(result);
                }
                catch (Exception e)
                {
                    thrownException = e;
                }
            }

            ExecuteFinallyCallbacks(ref thrownException);
            ClearCallbacks();

            if (thrownException != null)
            {
                throw new PromiseExecutionException(thrownException);
            }
        }
        
        public void Throw(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }
            
            if (!IsPending)
            {
                throw new InvalidOperationException($"Cannot complete a non-pending promise.");
            }

            IsPending = false;

            Exception thrownException = null;
            foreach (var callback in _catchCallbacks)
            {
                try
                {
                    callback(exception);
                }
                catch (Exception e)
                {
                    thrownException = e;
                }
            }
            
            ExecuteFinallyCallbacks(ref thrownException);
            ClearCallbacks();

            if (thrownException != null)
            {
                throw new PromiseExecutionException(exception);
            }
        }
        
        #endregion
        
        #region Transformation Functions
        
        public IPromise ContinueWith(
            Func<IPromise> transformResult,
            Func<Exception, IPromise> transformException = null)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }
            
            var newPromise = new Promise();

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                transformResult()
                    .Catch(e => newPromise.Throw(e))
                    .Then(() => newPromise.Complete());
            });

            return newPromise;
        }

        public IPromise ContinueWith(
            Func<T, IPromise> transformResult,
            Func<Exception, IPromise> transformException = null)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }
            
            var newPromise = new Promise();

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(result =>
            {
                transformResult(result)
                    .Catch(e => newPromise.Throw(e))
                    .Then(() => newPromise.Complete());
            });

            return newPromise;
        }

        public IPromise<U> ContinueWith<U>(
            Func<IPromise<U>> transformResult, 
            Func<Exception, IPromise<U>> transformException = null)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }
            
            var newPromise = new Promise<U>();

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(result =>
            {
                transformResult()
                    .Catch(e => newPromise.Throw(e))
                    .Then(result2 => newPromise.Complete(result2));
            });

            return newPromise;
        }
        
        public IPromise<U> ContinueWith<U>(
            Func<T, IPromise<U>> transformResult,
            Func<Exception, IPromise<U>> transformException = null)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }
            
            var newPromise = new Promise<U>();

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(result =>
            {
                transformResult(result)
                    .Catch(e => newPromise.Throw(e))
                    .Then(result2 => newPromise.Complete(result2));
            });

            return newPromise;
        }

        public IPromise<U> Transform<U>(Func<U> transformResult)
        {
            var newPromise = new Promise<U>();
            Catch(e => newPromise.Throw(e));
            Then(() => newPromise.Complete(transformResult()));
            return newPromise;
        }
        
        public IPromise<U> Transform<U>(Func<T, U> transformResult)
        {
            var newPromise = new Promise<U>();
            Catch(e => newPromise.Throw(e));
            Then(result => newPromise.Complete(transformResult(result)));
            return newPromise;        
        }

        IPromise IPromise.TransformException(Func<Exception, Exception> transformException)
        {
            return TransformException(transformException);
        }
        
        public IPromise<T> TransformException(Func<Exception, Exception> transformException)
        {
            var newPromise = new Promise<T>();
            Catch(e => newPromise.Throw(transformException(e)));
            Then(_result => newPromise.Complete(_result));
            return newPromise;
        }

        #endregion
        
        #region IEnumerator
        
        object IEnumerator.Current => null;

        // Allow coroutines to yield on the promise until it completes
        bool IEnumerator.MoveNext()
        {
            return IsPending;
        }

        void IEnumerator.Reset() { }
        
        #endregion

        protected void ExecuteFinallyCallbacks(ref Exception thrownException)
        {
            foreach (var callback in _finallyCallbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    thrownException = e;
                }
            }
        }
        
        protected virtual void ClearCallbacks()
        {
            // Clear all the subscribed callbacks free that memory
            // and make sure we don't hold onto any subscriber references
            // that might prevent GC from running
            _thenCallbacks.Clear();
            _catchCallbacks.Clear();
            _finallyCallbacks.Clear();
        }
    }
}