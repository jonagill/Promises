using System;
using System.Threading;
using NUnit.Framework;

namespace Promises
{
    public class TypelessCancelablePromiseTests
    {
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
    }
}
