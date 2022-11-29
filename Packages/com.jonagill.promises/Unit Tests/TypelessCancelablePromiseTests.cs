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
    }
}
