using System;
using System.Threading;
using NUnit.Framework;

namespace Promises
{
    public class TypelessCancelablePromiseTests
    {
        #region Basic Functionality
        
        [Test]
        public void CancelInvokesCallbacks()
        {
            var canceledRan = false;
            var promise = new CancelablePromise()
                .Canceled(() => canceledRan = true);
            
            Assert.IsFalse(canceledRan);

            promise.Cancel();

            Assert.IsTrue(canceledRan);
        }
        
        [Test]
        public void CanceledPromiseCallsCanceledImmediately()
        {
            var promise = new CancelablePromise();
            promise.Cancel();
            
            var canceledRan = false;
            promise.Canceled(() => canceledRan = true);
            
            Assert.IsTrue(canceledRan);
        }
        
        [Test]
        public void CancelTriggersFinally()
        {
            var finallyRan = false;
            var promise = new CancelablePromise();
            promise.Finally(() => finallyRan = true);
            
            Assert.IsFalse(finallyRan);
            
            promise.Cancel();
            
            Assert.IsTrue(finallyRan);
        }
        
        [Test]
        public void CanceledPromiseCallsFinallyImmediately()
        {
            var promise = new CancelablePromise();
            promise.Cancel();

            var finallyRan = false;
            promise.Finally(() => finallyRan = true);
            
            Assert.IsTrue(finallyRan);
        }
        
        [Test]
        public void CanceledCallbacksExecuteInOrder()
        {
            var callbacksExecuted = 0;
            var callbackResult0 = -1;
            var callbackResult1 = -1;
            var callbackResult2 = -1;
            
            var promise = new CancelablePromise();
            promise
                .Canceled(() =>
                {
                    callbackResult0 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Canceled(() =>
                {
                    callbackResult1 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Canceled(() =>
                {
                    callbackResult2 = callbacksExecuted;
                    callbacksExecuted++;
                });
            
            Assert.AreEqual(-1, callbackResult0);
            Assert.AreEqual(-1, callbackResult1);
            Assert.AreEqual(-1, callbackResult2);
            
            promise.Cancel();
            
            Assert.AreEqual(0, callbackResult0);
            Assert.AreEqual(1, callbackResult1);
            Assert.AreEqual(2, callbackResult2);
        }
        
        [Test]
        public void CanConstructPromiseWithCustomTokenSource()
        {
            var tokenSource = new CancellationTokenSource();
            var promise = new CancelablePromise(tokenSource);
            
            Assert.AreEqual(tokenSource.Token, promise.CancellationToken);
            Assert.IsFalse(promise.CancellationToken.IsCancellationRequested);
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsTrue(promise.IsPending);

            promise.Cancel();

            Assert.IsTrue(promise.CancellationToken.IsCancellationRequested);
            Assert.IsTrue(promise.IsCanceled);
            Assert.IsFalse(promise.IsPending);
        }
        
        [Test]
        public void CustomTokenSourceCanCancelPromiseExternally()
        {
            var canceledRan = false;
            var tokenSource = new CancellationTokenSource();
            var promise = new CancelablePromise(tokenSource)
                .Canceled(() => canceledRan = true);
            
            Assert.IsFalse(canceledRan);
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsTrue(promise.IsPending);

            tokenSource.Cancel();
            
            Assert.IsTrue(canceledRan);
            Assert.IsTrue(promise.IsCanceled);
            Assert.IsFalse(promise.IsPending);
        }
        
        [Test]
        public void CustomTokenCanCancelPromiseExternally()
        {
            var canceledRan = false;
            var tokenSource = new CancellationTokenSource();
            var promise = new CancelablePromise(tokenSource.Token)
                .Canceled(() => canceledRan = true);
            
            Assert.IsFalse(canceledRan);
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsTrue(promise.IsPending);

            tokenSource.Cancel();
            
            Assert.IsTrue(canceledRan);
            Assert.IsTrue(promise.IsCanceled);
            Assert.IsFalse(promise.IsPending);
        }

        [Test]
        public void CancelingPromiseSetsState()
        {
            var promise = new CancelablePromise();
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsTrue(promise.IsPending);
            
            promise.Cancel();
            Assert.IsTrue(promise.IsCanceled);
            Assert.IsFalse(promise.IsPending);
        }

        [Test]
        public void CompletingPromiseSetsState()
        {
            var promise = new CancelablePromise();
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsTrue(promise.IsPending);
            
            promise.Complete();
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsFalse(promise.IsPending);
        }
        
        [Test]
        public void ThrowingPromiseSetsState()
        {
            var promise = new CancelablePromise();
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsTrue(promise.IsPending);
            
            promise.Throw(new Exception());
            Assert.IsFalse(promise.IsCanceled);
            Assert.IsFalse(promise.IsPending);
        }
        
        #endregion
        
        #region Exceptions
        
        [Test]
        public void CanceledMustHaveCallback()
        {
            var promise = new CancelablePromise();
            Assert.Throws<ArgumentNullException>(() => promise.Canceled(null));
        }
        
        [Test]
        public void CannotCancelNonPendingPromise()
        {
            var promise = new CancelablePromise();
            promise.Complete();
            Assert.Throws<InvalidOperationException>(() => promise.Cancel());
        }
        
        [Test]
        public void CancelRethrowsInternalException()
        {
            var aRun = false;
            var bRun = false;
            var cRun = false;
            var internalException = new Exception("Internal");
            Exception caughtException = null;
            
            var promise = new CancelablePromise();
            promise
                .Canceled(() => aRun = true)
                .Canceled(() => throw internalException)
                .Canceled(() => cRun = true);
            
            Assert.IsFalse(aRun);
            Assert.IsFalse(bRun);
            Assert.IsFalse(cRun);
            Assert.IsNull(caughtException);

            try
            {
                promise.Cancel();
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            
            Assert.IsTrue(aRun);
            Assert.IsFalse(bRun);
            Assert.IsTrue(cRun);
            Assert.IsNotNull(caughtException);
            Assert.AreEqual(internalException, caughtException.InnerException);
        }
        
        [Test]
        public void CancelRethrowsInternalExceptionFromFinally()
        {
            var aRun = false;
            var bRun = false;
            var cRun = false;
            var internalException = new Exception("Internal");
            Exception caughtException = null;
            
            var promise = new CancelablePromise();
            promise
                .Finally(() => aRun = true)
                .Finally(() => throw internalException)
                .Finally(() => cRun = true);
            
            Assert.IsFalse(aRun);
            Assert.IsFalse(bRun);
            Assert.IsFalse(cRun);
            Assert.IsNull(caughtException);

            try
            {
                promise.Cancel();
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            
            Assert.IsTrue(aRun);
            Assert.IsFalse(bRun);
            Assert.IsTrue(cRun);
            Assert.IsNotNull(caughtException);
            Assert.AreEqual(internalException, caughtException.InnerException);        }
        
        [Test]
        public void OnlyFinalInternalExceptionIsRethrown()
        {
            var internalException = new Exception("Internal");
            Exception caughtException = null;
            
            var promise = new CancelablePromise();
            promise
                .Canceled(() => throw new Exception())
                .Canceled(() => throw new Exception())
                .Finally(() => throw new Exception())
                .Finally(() => throw internalException);
            
            Assert.IsNull(caughtException);

            try
            {
                promise.Cancel();
            }
            catch (Exception e)
            {
                caughtException = e;
            }
            
            Assert.IsNotNull(caughtException);
            Assert.AreEqual(internalException, caughtException.InnerException);
        }
        
        #endregion
        
        #region Transformation Functions
        
        [Test]
        public void ContinueWithChainsPromisesOnComplete()
        {
            var firstPromise = new CancelablePromise();
            Promise continuePromise = null;
            
            var bothPromisesCompleted = false;

            var fp = (ICancelablePromise)firstPromise;

            fp
                .ContinueWith(() =>
                {
                    continuePromise = new Promise();
                    return continuePromise;
                })
                .Then(() =>
                {
                    bothPromisesCompleted = true;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsFalse(bothPromisesCompleted);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsFalse(bothPromisesCompleted);
            
            continuePromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsTrue(bothPromisesCompleted);
        }
        
        [Test]
        public void ContinueWithSkipsOnThrow()
        {
            var firstPromise = new CancelablePromise();
            Promise continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new Promise();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(new Exception());
            
            // Second promise is never generated because the first promise
            // threw an exception
            Assert.IsNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void ContinueWithChainsPromisesOnThrow()
        {
            var firstPromise = new CancelablePromise();
            Promise continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new Promise();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNull(exception);
            
            continuePromise.Throw(new Exception());
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void ContinueWithCanTransformExceptionOnThrow()
        {
            var firstPromise = new CancelablePromise();
            Promise onCompletePromise = null;
            Promise onThrowPromise = null;
            Exception exception = null;

            var firstException = new Exception("First");
            var onThrowException = new Exception("Second");

            firstPromise
                .ContinueWith(onComplete:() =>
                {
                    onCompletePromise = new Promise();
                    return onCompletePromise;
                }, onThrow: e =>
                {
                    onThrowPromise = new Promise();
                    return onThrowPromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNull(onThrowPromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(firstException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.IsNull(exception);
            
            onThrowPromise.Throw(onThrowException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.AreEqual(onThrowException, exception);
        }
        
        [Test]
        public void TypedContinueWithChainsPromisesOnComplete()
        {
            var firstPromise = new CancelablePromise();
            Promise<int> continuePromise = null;
            var result = -1;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new Promise<int>();
                    return continuePromise;
                })
                .Then(r =>
                {
                    result = r;
                });
            
            Assert.IsNull(continuePromise);
            Assert.AreEqual(-1, result);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.AreEqual(-1, result);
            
            continuePromise.Complete(99);
            
            Assert.IsNotNull(continuePromise);
            Assert.AreEqual(99, result);
        }
        
        [Test]
        public void TypedContinueWithSkipsOnThrow()
        {
            var firstPromise = new CancelablePromise();
            Promise<int> continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new Promise<int>();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(new Exception());
            
            // Second promise is never generated because the first promise
            // threw an exception
            Assert.IsNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void TypedContinueWithChainsPromisesOnThrow()
        {
            var firstPromise = new CancelablePromise();
            Promise<int> continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new Promise<int>();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNull(exception);
            
            continuePromise.Throw(new Exception());
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void TypedContinueWithCanTransformExceptionOnThrow()
        {
            var firstPromise = new Promise();
            Promise<int> onCompletePromise = null;
            Promise<int> onThrowPromise = null;
            Exception exception = null;

            var firstException = new Exception("First");
            var onThrowException = new Exception("Second");

            firstPromise
                .ContinueWith(onComplete:() =>
                {
                    onCompletePromise = new Promise<int>();
                    return onCompletePromise;
                }, onThrow: e =>
                {
                    onThrowPromise = new Promise<int>();
                    return onThrowPromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNull(onThrowPromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(firstException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.IsNull(exception);
            
            onThrowPromise.Throw(onThrowException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.AreEqual(onThrowException, exception);
        }
        
        [Test]
        public void CancelableContinueWithChainsPromisesOnComplete()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise continuePromise = null;
            
            var bothPromisesCompleted = false;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new CancelablePromise();
                    return continuePromise;
                })
                .Then(() =>
                {
                    bothPromisesCompleted = true;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsFalse(bothPromisesCompleted);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsFalse(bothPromisesCompleted);
            
            continuePromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsTrue(bothPromisesCompleted);
        }
        
        [Test]
        public void CancelableContinueWithSkipsOnThrow()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new CancelablePromise();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(new Exception());
            
            // Second promise is never generated because the first promise
            // threw an exception
            Assert.IsNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void CancelableContinueWithChainsPromisesOnThrow()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new CancelablePromise();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNull(exception);
            
            continuePromise.Throw(new Exception());
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void CancelableContinueWithCanTransformExceptionOnThrow()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise onCompletePromise = null;
            CancelablePromise onThrowPromise = null;
            Exception exception = null;

            var firstException = new Exception("First");
            var onThrowException = new Exception("Second");

            firstPromise
                .ContinueWith(onComplete:() =>
                {
                    onCompletePromise = new CancelablePromise();
                    return onCompletePromise;
                }, onThrow: e =>
                {
                    onThrowPromise = new CancelablePromise();
                    return onThrowPromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNull(onThrowPromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(firstException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.IsNull(exception);
            
            onThrowPromise.Throw(onThrowException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.AreEqual(onThrowException, exception);
        }
        
        [Test]
        public void CancelingInitialPromiseCancelsContinueWithPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            firstPromise.Cancel();
            
            Assert.IsTrue(firstPromise.IsCanceled);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNull(chainedPromise);
        }
        
        [Test]
        public void CancelingContinueWithPromiseCancelsInitialPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            continueWithPromise.Cancel();
            
            Assert.IsTrue(firstPromise.IsCanceled);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNull(chainedPromise);
        }
        
        [Test]
        public void CancelingContinueWithPromiseCancelsChainedPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            firstPromise.Complete();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsPending);
            
            continueWithPromise.Cancel();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsCanceled);
        }
        
        [Test]
        public void CancelingChainedPromiseCancelsContinueWithPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            firstPromise.Complete();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsPending);
            
            continueWithPromise.Cancel();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsCanceled);
        }
        
        [Test]
        public void CancelableTypedContinueWithChainsPromisesOnComplete()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> continuePromise = null;
            var result = -1;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new CancelablePromise<int>();
                    return continuePromise;
                })
                .Then(r =>
                {
                    result = r;
                });
            
            Assert.IsNull(continuePromise);
            Assert.AreEqual(-1, result);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.AreEqual(-1, result);
            
            continuePromise.Complete(99);
            
            Assert.IsNotNull(continuePromise);
            Assert.AreEqual(99, result);
        }
        
        [Test]
        public void CancelableTypedContinueWithSkipsOnThrow()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new CancelablePromise<int>();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(new Exception());
            
            // Second promise is never generated because the first promise
            // threw an exception
            Assert.IsNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void CancelableTypedContinueWithChainsPromisesOnThrow()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> continuePromise = null;
            Exception exception = null;

            firstPromise
                .ContinueWith(() =>
                {
                    continuePromise = new CancelablePromise<int>();
                    return continuePromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(continuePromise);
            Assert.IsNull(exception);
            
            firstPromise.Complete();
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNull(exception);
            
            continuePromise.Throw(new Exception());
            
            Assert.IsNotNull(continuePromise);
            Assert.IsNotNull(exception);
        }
        
        [Test]
        public void CancelableTypedContinueWithCanTransformExceptionOnThrow()
        {
            var firstPromise = new Promise();
            Promise<int> onCompletePromise = null;
            Promise<int> onThrowPromise = null;
            Exception exception = null;

            var firstException = new Exception("First");
            var onThrowException = new Exception("Second");

            firstPromise
                .ContinueWith(onComplete:() =>
                {
                    onCompletePromise = new Promise<int>();
                    return onCompletePromise;
                }, onThrow: e =>
                {
                    onThrowPromise = new Promise<int>();
                    return onThrowPromise;
                })
                .Catch(e =>
                {
                    exception = e;
                });
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNull(onThrowPromise);
            Assert.IsNull(exception);
            
            firstPromise.Throw(firstException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.IsNull(exception);
            
            onThrowPromise.Throw(onThrowException);
            
            Assert.IsNull(onCompletePromise);
            Assert.IsNotNull(onThrowPromise);
            Assert.AreEqual(onThrowException, exception);
        }
        
        [Test]
        public void CancelingInitialPromiseCancelsTypedContinueWithPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise<int>();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            firstPromise.Cancel();
            
            Assert.IsTrue(firstPromise.IsCanceled);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNull(chainedPromise);
        }
        
        [Test]
        public void CancelingTypedContinueWithPromiseCancelsInitialPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise<int>();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            continueWithPromise.Cancel();
            
            Assert.IsTrue(firstPromise.IsCanceled);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNull(chainedPromise);
        }
        
        [Test]
        public void CancelingTypedContinueWithPromiseCancelsChainedPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise<int>();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            firstPromise.Complete();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsPending);
            
            continueWithPromise.Cancel();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsCanceled);
        }
        
        [Test]
        public void CancelingChainedPromiseCancelsTypedContinueWithPromise()
        {
            var firstPromise = new CancelablePromise();
            CancelablePromise<int> chainedPromise = null;
            var continueWithPromise = firstPromise
                .ContinueWith(onComplete: () =>
                {
                    chainedPromise = new CancelablePromise<int>();
                    return chainedPromise;
                });

            Assert.IsTrue(firstPromise.IsPending);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNull(chainedPromise);
            
            firstPromise.Complete();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsPending);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsPending);
            
            continueWithPromise.Cancel();
            
            Assert.IsTrue(firstPromise.HasSucceeded);
            Assert.IsTrue(continueWithPromise.IsCanceled);
            Assert.IsNotNull(chainedPromise);
            Assert.IsTrue(chainedPromise.IsCanceled);
        }
        
        #endregion
    }
}
