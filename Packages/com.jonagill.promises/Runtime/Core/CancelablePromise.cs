using System;
using System.Collections.Generic;
using System.Threading;

namespace Promises
{
    public class CancelablePromise : Promise, ICancelablePromise
    {
        public bool IsCanceled { get; private set; }
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
            
            CancelIfPending();
        }
        
        public void CancelIfPending()
        {
            if (!IsPending)
            {
                return;
            }

            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (Exception e)
            {
                // Exceptions thrown while canceling a TokenSource get wrapped
                // in an AggregateException
                if (e.InnerException != null)
                {
                    throw new PromiseExecutionException(e.InnerException);    
                }

                throw new PromiseExecutionException(e);
                
            }
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

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                // If we were canceled via another cancellation token other than our own
                // Flag our own cancellation token as canceled as well
                _cancellationTokenSource.Cancel();
            }
            
            if (thrownException != null)
            {
                throw thrownException;
            }
        }

        public new ICancelablePromise Then(Action onComplete)
        {
            return (ICancelablePromise) base.Then(onComplete);
        }

        public new ICancelablePromise Catch(Action<Exception> onThrow)
        {
            return (ICancelablePromise) base.Catch(onThrow);
        }

        public new ICancelablePromise Finally(Action onFinish)
        {
            return (ICancelablePromise) base.Finally(onFinish);
        }

        public ICancelablePromise Canceled(Action onCancel)
        {
            if (onCancel == null)
            {
                throw new ArgumentNullException(nameof(onCancel));
            }

            if (IsPending)
            {
                _canceledCallbacks.Add(onCancel);
            }
            else if (IsCanceled)
            {
                onCancel();
            }

            return this;        
        }
        
        #region Transformation Functions
        
        public IPromise ContinueWith(
            Func<IPromise> onComplete, 
            Func<IPromise> onCancel, 
            Func<Exception, IPromise> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            var newPromise = new Promise();
            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (onCancel != null)
                {
                    onCancel()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
            });

            this.Then(() =>
            {
                onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Then(() => newPromise.Complete());
            });

            return newPromise;
        }
        
        public IPromise ContinueWith(Func<IPromise> onComplete, Func<IPromise> onCancel)
        {
            return ContinueWith(onComplete, onCancel, null);
        }

        public IPromise<T> ContinueWith<T>(
            Func<IPromise<T>> onComplete, 
            Func<IPromise<T>> onCancel, 
            Func<Exception, IPromise<T>> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            var newPromise = new Promise<T>();
            
            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(r => newPromise.Complete(r));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (onCancel != null)
                {
                    onCancel()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(r => newPromise.Complete(r));
                }
            });

            this.Then(() =>
            {
                onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Then(r => newPromise.Complete(r));
            });

            return newPromise;
        }
        
        public IPromise<T> ContinueWith<T>(Func<IPromise<T>> onComplete, Func<IPromise<T>> onCancel)
        {
            return ContinueWith(onComplete, onCancel, null);
        }
        
        public ICancelablePromise ContinueWith(
            Func<ICancelablePromise> onComplete,
            Func<Exception, ICancelablePromise> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise newPromise = new CancelablePromise(_cancellationTokenSource);

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    var thrownPromise = onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.CancelIfPending())
                        .Then(() => newPromise.Complete());

                    // If this promise or the returned promise get canceled,
                    // chain that to the downstream promise
                    newPromise.Canceled(() => thrownPromise.CancelIfPending());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                var thenPromise = onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.CancelIfPending())
                    .Then(() => newPromise.Complete());
                
                // If this promise or the returned promise get canceled,
                // chain that to the downstream promise
                newPromise.Canceled(() => thenPromise.CancelIfPending());
            });
            
            return newPromise;        
        }
        
        public ICancelablePromise ContinueWith(Func<ICancelablePromise> onComplete)
        {
            return ContinueWith(onComplete, null);
        }
        
        public ICancelablePromise<T> ContinueWith<T>(
            Func<ICancelablePromise<T>> onComplete, 
            Func<Exception, ICancelablePromise<T>> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }

            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise<T> newPromise = new CancelablePromise<T>(_cancellationTokenSource);
            
            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    var thrownPromise = onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.CancelIfPending())
                        .Then(result => newPromise.Complete(result));
                    
                    // If this promise or the returned promise get canceled,
                    // chain that to the downstream promise
                    newPromise.Canceled(() => thrownPromise.CancelIfPending());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                var thenPromise = onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.CancelIfPending())
                    .Then(result => newPromise.Complete(result));
                
                // If this promise or the returned promise get canceled,
                // chain that to the downstream promise
                newPromise.Canceled(() => thenPromise.CancelIfPending());
            });

            return newPromise;   
        }
        
        public ICancelablePromise<T> ContinueWith<T>(Func<ICancelablePromise<T>> onComplete)
        {
            return ContinueWith(onComplete, null);
        }
        
        public new ICancelablePromise<T> Transform<T>(Func<T> transformResult)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise<T>(_cancellationTokenSource);
            Catch(e => newPromise.Throw(e));
            Then(() => newPromise.Complete(transformResult()));
            return newPromise;
        }

        public new ICancelablePromise TransformException(Func<Exception, Exception> transformException)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise(_cancellationTokenSource);
            Catch(e => newPromise.Throw(transformException(e)));
            Then(() => newPromise.Complete());
            return newPromise;        
        }

        #endregion
        
        #region IReadOnlyCancelablePromise
        
        
        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Canceled(Action onCancel)
        {
            return Canceled(onCancel);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Then(Action onComplete)
        {
            return Then(onComplete);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Catch(Action<Exception> onThrow)
        {
            return Catch(onThrow);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Finally(Action onFinish)
        {
            return Finally(onFinish);
        }

        IReadOnlyCancelablePromise<T> IReadOnlyCancelablePromise.Transform<T>(Func<T> transformResult)
        {
            return Transform(transformResult);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.TransformException(Func<Exception, Exception> transformResult)
        {
            return TransformException(transformResult);
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
        public bool IsCanceled { get; private set; }
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
            
            CancelIfPending();
        }
        
        public void CancelIfPending()
        {
            if (!IsPending)
            {
                return;
            }

            try
            {
                _cancellationTokenSource.Cancel();
            }
            catch (Exception e)
            {
                // Exceptions thrown while canceling a TokenSource get wrapped
                // in an AggregateException
                if (e.InnerException != null)
                {
                    throw new PromiseExecutionException(e.InnerException);    
                }

                throw new PromiseExecutionException(e);
            }
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

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                // If we were canceled via another cancellation token other than our own
                // Flag our own cancellation token as canceled as well
                _cancellationTokenSource.Cancel();
            }
            
            if (thrownException != null)
            {
                // Don't bother wrapping in a PromiseExecutionException
                // The CancellationTokenSource code will automatically
                // wrap our exception in an AggregateException
                throw thrownException;
            }
        }
        
        public new ICancelablePromise Then(Action<T> onComplete)
        {
            return (ICancelablePromise) base.Then(onComplete);
        }
        
        public new ICancelablePromise<T> Then(Action onComplete)
        {
            return (ICancelablePromise<T>) base.Then(onComplete);
        }

        public new ICancelablePromise<T> Catch(Action<Exception> onThrow)
        {
            return (ICancelablePromise<T>) base.Catch(onThrow);
        }

        public new ICancelablePromise<T> Finally(Action onFinish)
        {
            return (ICancelablePromise<T>) base.Finally(onFinish);
        }

        public ICancelablePromise<T> Canceled(Action onCancel)
        {
            if (onCancel == null)
            {
                throw new ArgumentNullException(nameof(onCancel));
            }

            if (IsPending)
            {
                _canceledCallbacks.Add(onCancel);
            }
            else if (IsCanceled)
            {
                onCancel();
            }

            return this;
        }

        #region ICancelablePromise

        ICancelablePromise ICancelablePromise.Canceled(Action onCancel)
        {
            return Canceled(onCancel);
        }

        ICancelablePromise ICancelablePromise.Then(Action onComplete)
        {
            return Then(onComplete);
        }

        ICancelablePromise ICancelablePromise.Catch(Action<Exception> onThrow)
        {
            return Catch(onThrow);
        }

        ICancelablePromise ICancelablePromise.Finally(Action onFinish)
        {
            return Finally(onFinish);
        }

        #endregion
        
        #region Transformation Functions
        
        public IPromise ContinueWith(
            Func<IPromise> onComplete, 
            Func<IPromise> onCancel, 
            Func<Exception, IPromise> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            Promise newPromise = new Promise();

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (onCancel != null)
                {
                    onCancel()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
            });

            this.Then(() =>
            {
                onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Then(() => newPromise.Complete());
            });

            return newPromise;
        }

        public IPromise ContinueWith(Func<IPromise> onComplete, Func<IPromise> onCancel)
        {
            return ContinueWith(onComplete, onCancel, null);
        }

        public IPromise ContinueWith(
            Func<T, IPromise> onComplete, 
            Func<IPromise> onCancel, 
            Func<Exception, IPromise> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise newPromise = new CancelablePromise(_cancellationTokenSource);

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (onCancel != null)
                {
                    onCancel()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(() => newPromise.Complete());
                }
            });

            this.Then(result =>
            {
                onComplete(result)
                    .Catch(e => newPromise.Throw(e))
                    .Then(() => newPromise.Complete());
            });

            return newPromise;
        }
        
        public IPromise ContinueWith(Func<T, IPromise> onComplete, Func<IPromise> onCancel)
        {
            return ContinueWith(onComplete, onCancel, null);
        }
        
        public IPromise<U> ContinueWith<U>(
            Func<IPromise<U>> onComplete, 
            Func<IPromise<U>> onCancel, 
            Func<Exception, IPromise<U>> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            Promise<U> newPromise = new Promise<U>();

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (onCancel != null)
                {
                    onCancel()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
            });

            this.Then(result =>
            {
                onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Then(r2 => newPromise.Complete(r2));
            });

            return newPromise;
        }
        
        public IPromise<U> ContinueWith<U>(Func<IPromise<U>> onComplete, Func<IPromise<U>> onCancel)
        {
            return ContinueWith(onComplete, onCancel, null);
        }
        
        public IPromise<U> ContinueWith<U>(
            Func<T, IPromise<U>> onComplete, 
            Func<IPromise<U>> onCancel, 
            Func<Exception, IPromise<U>> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            Promise<U> newPromise = new Promise<U>();

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
                else
                {
                    newPromise.Throw(e);
                }
            });
            
            this.Canceled(() =>
            {
                if (onCancel != null)
                {
                    onCancel()
                        .Catch(e2 => newPromise.Throw(e2))
                        .Then(result => newPromise.Complete(result));
                }
            });

            this.Then(result =>
            {
                onComplete(result)
                    .Catch(e => newPromise.Throw(e))
                    .Then(r2 => newPromise.Complete(r2));
            });

            return newPromise;
        }
        
        public IPromise<U> ContinueWith<U>(Func<T, IPromise<U>> onComplete, Func<IPromise<U>> onCancel)
        {
            return ContinueWith(onComplete, onCancel, null);
        }

        
        public ICancelablePromise ContinueWith(
            Func<ICancelablePromise> onComplete, 
            Func<Exception, ICancelablePromise> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise newPromise = new CancelablePromise(_cancellationTokenSource);

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    var thrownPromise = onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.CancelIfPending())
                        .Then(() => newPromise.Complete());
                    
                    // If this promise or the returned promise get canceled,
                    // chain that to the downstream promise
                    newPromise.Canceled(() => thrownPromise.CancelIfPending());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                var thenPromise = onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.CancelIfPending())
                    .Then(() => newPromise.Complete());
                
                // If this promise or the returned promise get canceled,
                // chain that to the downstream promise
                newPromise.Canceled(() => thenPromise.CancelIfPending());
            });

            return newPromise;
        }
         
        public ICancelablePromise ContinueWith(Func<ICancelablePromise> onComplete)
        {
            return ContinueWith(onComplete, null);
        }

        public ICancelablePromise<U> ContinueWith<U>(
            Func<ICancelablePromise<U>> onComplete, 
            Func<Exception, ICancelablePromise<U>> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise<U> newPromise = new CancelablePromise<U>(_cancellationTokenSource);

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    var thrownPromise = onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.CancelIfPending())
                        .Then(result => newPromise.Complete(result));
                    
                    // If this promise or the returned promise get canceled,
                    // chain that to the downstream promise
                    newPromise.Canceled(() => thrownPromise.CancelIfPending());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(() =>
            {
                var thenPromise = onComplete()
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.CancelIfPending())
                    .Then(result => newPromise.Complete(result));
                
                // If this promise or the returned promise get canceled,
                // chain that to the downstream promise
                newPromise.Canceled(() => thenPromise.CancelIfPending());
            });

            return newPromise;
        }
        public ICancelablePromise<U> ContinueWith<U>(Func<ICancelablePromise<U>> onComplete)
        {
            return ContinueWith(onComplete, null);
        }

        public ICancelablePromise ContinueWith(
            Func<T, ICancelablePromise> onComplete, 
            Func<Exception, ICancelablePromise> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise newPromise = new CancelablePromise(_cancellationTokenSource);

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    var thrownPromise = onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.CancelIfPending())
                        .Then(() => newPromise.Complete());
                    
                    // If this promise or the returned promise get canceled,
                    // chain that to the downstream promise
                    newPromise.Canceled(() => thrownPromise.CancelIfPending());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(result =>
            {
                var thenPromise = onComplete(result)
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.CancelIfPending())
                    .Then(() => newPromise.Complete());
                
                // If this promise or the returned promise get canceled,
                // chain that to the downstream promise
                newPromise.Canceled(() => thenPromise.CancelIfPending());
            });

            return newPromise;
        }
        
        public ICancelablePromise ContinueWith(Func<T, ICancelablePromise> onComplete)
        {
            return ContinueWith(onComplete, null);
        }

        public ICancelablePromise<U> ContinueWith<U>(
            Func<T, ICancelablePromise<U>> onComplete, 
            Func<Exception, ICancelablePromise<U>> onThrow)
        {
            if (onComplete == null)
            {
                throw new ArgumentNullException(nameof(onComplete));
            }
            
            // Create a new promise with our own cancellation source
            // so that canceling that promise cancels us (and vice versa)
            CancelablePromise<U> newPromise = new CancelablePromise<U>(_cancellationTokenSource);

            this.Catch(e =>
            {
                if (onThrow != null)
                {
                    var thrownPromise = onThrow(e)
                        .Catch(e2 => newPromise.Throw(e2))
                        .Canceled(() => newPromise.CancelIfPending())
                        .Then(result => newPromise.Complete(result));
                    
                    // If this promise or the returned promise get canceled,
                    // chain that to the downstream promise
                    newPromise.Canceled(() => thrownPromise.CancelIfPending());
                }
                else
                {
                    newPromise.Throw(e);
                }
            });

            this.Then(result =>
            {
                var thenPromise = onComplete(result)
                    .Catch(e => newPromise.Throw(e))
                    .Canceled(() => newPromise.CancelIfPending())
                    .Then(r2 => newPromise.Complete(r2));
                
                // If this promise or the returned promise get canceled,
                // chain that to the downstream promise
                newPromise.Canceled(() => thenPromise.CancelIfPending());
            });

            return newPromise;
        }
        
        public ICancelablePromise<U> ContinueWith<U>(Func<T, ICancelablePromise<U>> onComplete)
        {
            return ContinueWith(onComplete, null);
        }

        public new ICancelablePromise<U> Transform<U>(Func<U> transformResult)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise<U>(_cancellationTokenSource);
            Catch(e => newPromise.Throw(e));
            Then(() => newPromise.Complete(transformResult()));
            return newPromise;
        }

        public new ICancelablePromise<U> Transform<U>(Func<T, U> transformResult)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise<U>(_cancellationTokenSource);
            Catch(e => newPromise.Throw(e));
            Then(result => newPromise.Complete(transformResult(result)));
            return newPromise;
        }
        
        ICancelablePromise ICancelablePromise.TransformException(Func<Exception, Exception> transformException)
        {
            return TransformException(transformException);
        }

        public new ICancelablePromise<T> TransformException(Func<Exception, Exception> transformException)
        {
            // Pass our token source to the new promise so that cancelling either promise cancels the other
            var newPromise = new CancelablePromise<T>(_cancellationTokenSource);
            Catch(e => newPromise.Throw(transformException(e)));
            Then(result => newPromise.Complete(result));
            return newPromise;
        }
        
        #endregion

        #region IReadOnlyCancelablePromise

        IReadOnlyCancelablePromise<T> IReadOnlyCancelablePromise<T>.Canceled(Action onCancel)
        {
            return Canceled(onCancel);
        }

        IReadOnlyCancelablePromise<T> IReadOnlyCancelablePromise<T>.Then(Action onComplete)
        {
            return Then(onComplete);
        }

        IReadOnlyCancelablePromise<T> IReadOnlyCancelablePromise<T>.Catch(Action<Exception> onThrow)
        {
            return Catch(onThrow);
        }

        IReadOnlyCancelablePromise<T> IReadOnlyCancelablePromise<T>.Finally(Action onFinish)
        {
            return Finally(onFinish);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise<T>.Then(Action<T> onComplete)
        {
            return Then(onComplete);
        }
        
        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Canceled(Action onCancel)
        {
            return Canceled(onCancel);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Then(Action onComplete)
        {
            return Then(onComplete);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Catch(Action<Exception> onThrow)
        {
            return Catch(onThrow);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.Finally(Action onFinish)
        {
            return Finally(onFinish);
        }
        
        IReadOnlyCancelablePromise<U> IReadOnlyCancelablePromise<T>.Transform<U>(Func<T, U> transformResult)
        {
            return Transform(transformResult);
        }

        IReadOnlyCancelablePromise<U> IReadOnlyCancelablePromise.Transform<U>(Func<U> transformResult)
        {
            return Transform(transformResult);
        }

        IReadOnlyCancelablePromise IReadOnlyCancelablePromise.TransformException(Func<Exception, Exception> transformException)
        {
            return TransformException(transformException);
        }
        
        
        IReadOnlyCancelablePromise<T> IReadOnlyCancelablePromise<T>.TransformException(Func<Exception, Exception> transformException)
        {
            return TransformException(transformException);
        }

        #endregion
    }
}