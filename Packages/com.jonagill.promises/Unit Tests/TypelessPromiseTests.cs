using System;
using NUnit.Framework;

namespace Promises
{
    public class TypelessPromiseTests
    {
        [Test]
        public void CompleteTriggersThen()
        {
            var thenRan = false;
            var promise = new Promise();
            promise.Then(() => thenRan = true);
            
            Assert.IsFalse(thenRan);
            
            promise.Complete();
            
            Assert.IsTrue(thenRan);
        }
        
        [Test]
        public void CompletedPromiseCallsThenImmediately()
        {
            var promise = new Promise();
            promise.Complete();

            var thenRan = false;
            promise.Then(() => thenRan = true);
            
            Assert.IsTrue(thenRan);
        }
        
        [Test]
        public void ThrowTriggersCatch()
        {
            Exception exceptionThrown = null;
            Exception exceptionToThrow = new Exception("Error");
            
            var promise = new Promise();
            promise.Catch(e => exceptionThrown = e);
            
            Assert.AreEqual(null, exceptionThrown);
            
            promise.Throw(exceptionToThrow);
            
            Assert.AreEqual(exceptionToThrow, exceptionThrown);
        }
        
        [Test]
        public void ThrownPromiseCallsCatchImmediately()
        {
            Exception exceptionThrown = null;
            Exception exceptionToThrow = new Exception("Error");
            
            var promise = new Promise();
            promise.Throw(exceptionToThrow);
            promise.Catch(e => exceptionThrown = e);

            Assert.AreEqual(exceptionToThrow, exceptionThrown);
        }

        
        [Test]
        public void CompleteTriggersFinally()
        {
            var finallyRan = false;
            var promise = new Promise();
            promise.Finally(() => finallyRan = true);
            
            Assert.IsFalse(finallyRan);
            
            promise.Complete();
            
            Assert.IsTrue(finallyRan);
        }
        
        [Test]
        public void CompletedPromiseCallsFinallyImmediately()
        {
            var promise = new Promise();
            promise.Complete();

            var finallyRan = false;
            promise.Finally(() => finallyRan = true);
            
            Assert.IsTrue(finallyRan);
        }
        
        [Test]
        public void ThrowTriggersFinally()
        {
            var finallyRan = false;
            var promise = new Promise();
            promise.Catch(e => finallyRan = true);
            
            Assert.IsFalse(finallyRan);
            
            promise.Throw(new Exception());
            
            Assert.IsTrue(finallyRan);
        }
        
        [Test]
        public void ThrownPromiseCallsFinallyImmediately()
        {
            var finallyRan = false;
            var promise = new Promise();
            promise.Throw(new Exception());
            promise.Catch(e => finallyRan = true);

            Assert.IsTrue(finallyRan);
        }
        
        [Test]
        public void ThenCallbacksExecuteInOrder()
        {
            var callbacksExecuted = 0;
            var callbackResult0 = -1;
            var callbackResult1 = -1;
            var callbackResult2 = -1;
            
            var promise = new Promise();
            promise
                .Then(() =>
                {
                    callbackResult0 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Then(() =>
                {
                    callbackResult1 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Then(() =>
                {
                    callbackResult2 = callbacksExecuted;
                    callbacksExecuted++;
                });
            
            Assert.AreEqual(-1, callbackResult0);
            Assert.AreEqual(-1, callbackResult1);
            Assert.AreEqual(-1, callbackResult2);
            
            promise.Complete();
            
            Assert.AreEqual(0, callbackResult0);
            Assert.AreEqual(1, callbackResult1);
            Assert.AreEqual(2, callbackResult2);
        }
        
        [Test]
        public void CatchCallbacksExecuteInOrder()
        {
            var callbacksExecuted = 0;
            var callbackResult0 = -1;
            var callbackResult1 = -1;
            var callbackResult2 = -1;
            
            var promise = new Promise();
            promise
                .Catch(e =>
                {
                    callbackResult0 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Catch(e =>
                {
                    callbackResult1 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Catch(e =>
                {
                    callbackResult2 = callbacksExecuted;
                    callbacksExecuted++;
                });
            
            Assert.AreEqual(-1, callbackResult0);
            Assert.AreEqual(-1, callbackResult1);
            Assert.AreEqual(-1, callbackResult2);
            
            promise.Throw(new Exception());
            
            Assert.AreEqual(0, callbackResult0);
            Assert.AreEqual(1, callbackResult1);
            Assert.AreEqual(2, callbackResult2);
        }
        
        [Test]
        public void FinallyCallbacksExecuteInOrder()
        {
            var callbacksExecuted = 0;
            var callbackResult0 = -1;
            var callbackResult1 = -1;
            var callbackResult2 = -1;
            
            var promise = new Promise();
            promise
                .Finally(() =>
                {
                    callbackResult0 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Finally(() =>
                {
                    callbackResult1 = callbacksExecuted;
                    callbacksExecuted++;
                })
                .Finally(() =>
                {
                    callbackResult2 = callbacksExecuted;
                    callbacksExecuted++;
                });
            
            Assert.AreEqual(-1, callbackResult0);
            Assert.AreEqual(-1, callbackResult1);
            Assert.AreEqual(-1, callbackResult2);
            
            promise.Complete();
            
            Assert.AreEqual(0, callbackResult0);
            Assert.AreEqual(1, callbackResult1);
            Assert.AreEqual(2, callbackResult2);
        }

        [Test]
        public void ContinueWithChainsPromisesOnComplete()
        {
            var firstPromise = new Promise();
            Promise continuePromise = null;
            
            var bothPromisesCompleted = false;

            firstPromise
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
            var firstPromise = new Promise();
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
            var firstPromise = new Promise();
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
            var firstPromise = new Promise();
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
            var firstPromise = new Promise();
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
            var firstPromise = new Promise();
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
            var firstPromise = new Promise();
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
        public void TransformResultTransformsResult()
        {
            var promise = new Promise();
            int result = -1;

            promise
                .Transform(() => 1)
                .Then(r => result = r);
            
            Assert.AreEqual(-1, result);
            
            promise.Complete();
            
            Assert.AreEqual(1, result);
        }
        
        [Test]
        public void TransformResultPassesOnThrow()
        {
            var promise = new Promise();
            Exception exception = null;
            Exception exceptionToThrow = new Exception();

            promise
                .Transform(() => 1)
                .Catch(e => exception = e);
            
            Assert.IsNull(exception);

            promise.Throw(exceptionToThrow);
            
            Assert.AreEqual(exceptionToThrow, exception);
        }
        
        [Test]
        public void TransformExceptionTransformsException()
        {
            var promise = new Promise();
            Exception exception = null;
            Exception exceptionToThrow = new Exception("First");
            Exception tranformedException = new Exception("Transformed");

            promise
                .TransformException(e => tranformedException)
                .Catch(e => exception = e);
            
            Assert.IsNull(exception);

            promise.Throw(exceptionToThrow);
            
            Assert.AreEqual(tranformedException, exception);
        }
        
        [Test]
        public void TransformExceptionPassesOnComplete()
        {
            var promise = new Promise();
            Exception exception = null;
            bool thenRan = false;
            Exception tranformedException = new Exception("Transformed");

            promise
                .TransformException(e => tranformedException)
                .Catch(e => exception = e)
                .Then(() => thenRan = true);
            
            Assert.IsNull(exception);
            Assert.IsFalse(thenRan);

            promise.Complete();
            
            Assert.IsNull(exception);
            Assert.IsTrue(thenRan);
        }
        
        [Test]
        public void CompleteSetsState()
        {
            var promise = new Promise();
            
            Assert.IsTrue(promise.IsPending);
            Assert.IsFalse(promise.HasSucceeded);
            Assert.IsFalse(promise.HasException);

            promise.Complete();
            
            Assert.IsFalse(promise.IsPending);
            Assert.IsTrue(promise.HasSucceeded);
            Assert.IsFalse(promise.HasException);
        }
        
        [Test]
        public void ThrowSetsState()
        {
            var promise = new Promise();
            
            Assert.IsTrue(promise.IsPending);
            Assert.IsFalse(promise.HasSucceeded);
            Assert.IsFalse(promise.HasException);

            promise.Throw(new Exception());
            
            Assert.IsFalse(promise.IsPending);
            Assert.IsFalse(promise.HasSucceeded);
            Assert.IsTrue(promise.HasException);
        }
        
        #region Helper Tests
        
        [Test]
        public void CompletedPromiseWorks()
        {
            var thenRan = false;
            Promise.CompletedPromise.Then(() => thenRan = true);
            
            Assert.IsTrue(thenRan);
        }
        
        #endregion
    }
}
