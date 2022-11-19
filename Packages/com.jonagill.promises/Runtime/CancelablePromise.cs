using System;
using System.Collections.Generic;
using System.Threading;

namespace Promises
{
    public class CancelablePromise : Promise, ICancelablePromise
    {
        public bool IsCanceled { get; private set; }
        public bool CanBeCanceled => IsPending && _cancellationTokenSource != null;

        private readonly List<Action> _canceledCallbacks = new List<Action>();

        private CancellationTokenSource _cancellationTokenSource;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        /// <summary>
        /// Create a new cancelable promise with its own CancellationTokenSource.
        /// </summary>
        public CancelablePromise()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(OnCancellation);
        }

        /// <summary>
        /// Create a new cancelable promise with the given CancellationTokenSource.
        /// </summary>
        public CancelablePromise(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
            _cancellationTokenSource.Token.Register(OnCancellation);
        }
        
        /// <summary>
        /// Create a new cancelable promise with a given cancellationToken.
        /// This promise will cancel automatically if this token is canceled or
        /// if its own internal token is canceled.
        /// </summary>
        public CancelablePromise(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(OnCancellation);
            cancellationToken.Register(OnCancellation);
        }
        
        public void Cancel()
        {
            if (!IsPending)
            {
                throw new InvalidOperationException($"Cannot cancel a non-pending promise.");
            }

            _cancellationTokenSource.Cancel();
        }

        private void OnCancellation()
        {
            if (!IsPending)
            {
                return;
            }
            
            IsPending = false;
            IsCanceled = true;

            Exception thrownException = null;
            foreach (var callback in _canceledCallbacks)
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

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                // If we were canceled via another cancellation token other than our own
                // Flag our own cancellation token as canceled as well
                _cancellationTokenSource.Cancel();
            }
        }
        
        public ICancelablePromise Then(Action onComplete)
        {
            return (ICancelablePromise) base.Then(onComplete);
        }

        public ICancelablePromise Catch(Action<Exception> onThrow)
        {
            return (ICancelablePromise) base.Catch(onThrow);
        }

        public ICancelablePromise Finally(Action onFinish)
        {
            return (ICancelablePromise) base.Finally(onFinish);
        }

        public ICancelablePromise Canceled(Action onCancel)
        {
            if (onCancel == null)
            {
                throw new ArgumentNullException(nameof(onCancel));
            }

            if (IsCanceled)
            {
                _canceledCallbacks.Add(onCancel);
            }
            else
            {
                onCancel();
            }

            return this;        
        }
        
        #region Transformation Methods

        public ICancelablePromise ContinueWith(
            Func<ICancelablePromise> transformResult,
            Func<ICancelablePromise> transformCanceled = null,
            Func<Exception, ICancelablePromise> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }
            
            CancelablePromise newPromise;
            switch (chaining)
            {
                case CancellationChainingType.CancelAll:
                    // Create a new promise with our own cancellation source
                    // so that canceling that promise cancels us as well
                    newPromise = new CancelablePromise(_cancellationTokenSource);
                    break;
                case CancellationChainingType.CancelChildren:
                    // Create a new promise with our own cancellation token
                    // so that canceling this promise cancels the new promise as well
                    newPromise = new CancelablePromise(CancellationToken);
                    break;
                case CancellationChainingType.DontChain:
                    // Create a wholly new promise that does not chain with this promise
                    newPromise = new CancelablePromise();
                    break;
                default:
                    throw new ArgumentException(nameof(chaining));
            }

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.Cancel())
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (transformCanceled != null)
                {
                    transformCanceled()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.Cancel())
                        .Then(() => newPromise.Complete());
                }
            });

            this.Then(() =>
            {
                transformResult()
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.Cancel())
                    .Then(() => newPromise.Complete());
            });

            return newPromise;        
        }

        public ICancelablePromise<T> ContinueWith<T>(Func<ICancelablePromise<T>> transformResult, Func<ICancelablePromise> transformCanceled = null, Func<Exception, ICancelablePromise<T>> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll)
        {
            throw new NotImplementedException();
        }

        public ICancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> transformResult, 
            Func<ICancelablePromise<T>> transformCanceled = null,
            Func<Exception, ICancelablePromise<T>> transformException = null,
            CancellationChainingType chaining = CancellationChainingType.CancelAll)
        {
            if (transformResult == null)
            {
                throw new ArgumentNullException(nameof(transformResult));
            }
            
            CancelablePromise<T> newPromise;
            switch (chaining)
            {
                case CancellationChainingType.CancelAll:
                    // Create a new promise with our own cancellation source
                    // so that canceling that promise cancels us as well
                    newPromise = new CancelablePromise<T>(_cancellationTokenSource);
                    break;
                case CancellationChainingType.CancelChildren:
                    // Create a new promise with our own cancellation token
                    // so that canceling this promise cancels the new promise as well
                    newPromise = new CancelablePromise<T>(CancellationToken);
                    break;
                case CancellationChainingType.DontChain:
                    // Create a wholly new promise that does not chain with this promise
                    newPromise = new CancelablePromise<T>();
                    break;
                default:
                    throw new ArgumentException(nameof(chaining));
            }

            this.Catch(e =>
            {
                if (transformException != null)
                {
                    transformException(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.Cancel())
                        .Then(result => newPromise.Complete(result));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (transformCanceled != null)
                {
                    transformCanceled()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.Cancel())
                        .Then(result => newPromise.Complete(result));
                }
            });

            this.Then(() =>
            {
                transformResult()
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.Cancel())
                    .Then(result => newPromise.Complete(result));
            });

            return newPromise;   
        }

        public ICancelablePromise<T> Transform<T>(Func<T> transformResult)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise<T>(_cancellationTokenSource);
            Catch(e => newPromise.Throw(e));
            Then(() => newPromise.Complete(transformResult()));
            return newPromise;
            
        }

        public ICancelablePromise TransformException(Func<Exception, Exception> transformException)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise(_cancellationTokenSource);
            Catch(e => newPromise.Throw(transformException(e)));
            Then(() => newPromise.Complete());
            return newPromise;        
        }
        
        #endregion

        protected override void ClearCallbacks()
        {
            base.ClearCallbacks();
            _canceledCallbacks.Clear();
        }
    }

    public class CancelablePromise<T> : Promise<T>, ICancelablePromise<T>
    {
       
    }
}