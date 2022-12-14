using System;
using NUnit.Framework;

namespace Promises
{
    public class AllPromiseTests
    {
        [Test]
        public void SucceedsOnceAllPromiseCompletes()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var allPromise = Promise.All(promiseA, promiseB, promiseC);
            
            promiseA.Complete();
            
            Assert.IsFalse(allPromise.HasSucceeded);
            
            promiseB.Complete();
            
            Assert.IsFalse(allPromise.HasSucceeded);
            
            promiseC.Complete();
            
            Assert.IsTrue(allPromise.HasSucceeded);
        }
        
        [Test]
        public void ThrowsIfAnyPromisesThrow()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var allPromise = Promise.All(promiseA, promiseB, promiseC);
            
            promiseA.Complete();
            
            Assert.IsFalse(allPromise.HasException);
            
            promiseB.Throw(new Exception());
            
            Assert.IsTrue(allPromise.HasException);
            
            promiseC.Complete();
            
            Assert.IsTrue(allPromise.HasException);
        }
        
        [Test]
        public void CollectionVersionSucceedsOnceAllPromiseCompletes()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var allPromise = Promise.All(new IPromise[] {promiseA, promiseB, promiseC});
            
            promiseA.Complete();
            
            Assert.IsFalse(allPromise.HasSucceeded);
            
            promiseB.Complete();
            
            Assert.IsFalse(allPromise.HasSucceeded);
            
            promiseC.Complete();
            
            Assert.IsTrue(allPromise.HasSucceeded);
        }
        
        [Test]
        public void CollectionVersionThrowsIfAnyPromisesThrow()
        {
            var promiseA = new Promise();
            var promiseB = new Promise();
            var promiseC = new Promise();

            var allPromise = Promise.All(new IPromise[] {promiseA, promiseB, promiseC});
            
            promiseA.Complete();
            
            Assert.IsFalse(allPromise.HasException);
            
            promiseB.Throw(new Exception());
            
            Assert.IsTrue(allPromise.HasException);
            
            promiseC.Complete();
            
            Assert.IsTrue(allPromise.HasException);
        }
    }
}
