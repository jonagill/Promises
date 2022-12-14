using System;
using NUnit.Framework;

namespace Promises
{
    public class AnyPromiseTests
    {
        [Test]
        public void SucceedsOnceOnePromiseCompletes()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var anyPromise = Promise.Any(promiseA, promiseB, promiseC);
            
            promiseA.Throw(new Exception());
            
            Assert.IsFalse(anyPromise.HasSucceeded);
            
            promiseB.Complete();
            
            Assert.IsTrue(anyPromise.HasSucceeded);
            
            promiseC.Throw(new Exception());
            
            Assert.IsTrue(anyPromise.HasSucceeded);
        }
        
        [Test]
        public void ThrowsIfAllPromisesThrow()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var anyPromise = Promise.Any(promiseA, promiseB, promiseC);
            
            promiseA.Throw(new Exception());
            
            Assert.IsFalse(anyPromise.HasException);
            
            promiseB.Throw(new Exception());
            
            Assert.IsFalse(anyPromise.HasException);
            
            promiseC.Throw(new Exception());
            
            Assert.IsTrue(anyPromise.HasException);
        }
        
        [Test]
        public void CollectionVersionSucceedsOnceOnePromiseCompletes()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var anyPromise = Promise.Any(new IPromise[] {promiseA, promiseB, promiseC});

            promiseA.Throw(new Exception());
            
            Assert.IsFalse(anyPromise.HasSucceeded);
            
            promiseB.Complete();
            
            Assert.IsTrue(anyPromise.HasSucceeded);
            
            promiseC.Throw(new Exception());
            
            Assert.IsTrue(anyPromise.HasSucceeded);
        }
        
        [Test]
        public void CollectionVersionThrowsIfAllPromisesThrow()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var anyPromise = Promise.Any(new IPromise[] {promiseA, promiseB, promiseC});
            
            promiseA.Throw(new Exception());
            
            Assert.IsFalse(anyPromise.HasException);
            
            promiseB.Throw(new Exception());
            
            Assert.IsFalse(anyPromise.HasException);
            
            promiseC.Throw(new Exception());
            
            Assert.IsTrue(anyPromise.HasException);
        }
    }
}
