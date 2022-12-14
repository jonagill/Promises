using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Promises
{
    public class CombinePromiseTests
    {
        [Test]
        public void SucceedsOnceAllPromiseCompletes()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<string>();

            (int, int, string) result = default;
            
            var allPromise = Promise.Combine(promiseA, promiseB, promiseC);
            allPromise.Then(r =>
            {
                result = r;
            });
            
            promiseA.Complete(1);
            
            Assert.IsFalse(allPromise.HasSucceeded);
            
            promiseB.Complete(2);
            
            Assert.IsFalse(allPromise.HasSucceeded);
            
            promiseC.Complete("Result");
            
            Assert.IsTrue(allPromise.HasSucceeded);
            Assert.AreEqual(1, result.Item1);
            Assert.AreEqual(2, result.Item2);
            Assert.AreEqual("Result", result.Item3);
        }
        
        [Test]
        public void ThrowsIfAnyPromisesThrow()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<string>();

            var allPromise = Promise.Combine(promiseA, promiseB, promiseC);
            
            promiseA.Complete(1);
            
            Assert.IsFalse(allPromise.HasException);
            
            promiseB.Throw(new Exception());
            
            Assert.IsTrue(allPromise.HasException);
            
            promiseC.Complete("Result");
            
            Assert.IsTrue(allPromise.HasException);
        }
        
        
        [Test]
        public void CollectionVersionSucceedsOnceAllPromiseCompletes()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<int>();

            IList<int> result = default;
            
            var combinePromise = Promise.Combine(new IPromise<int>[] {promiseA, promiseB, promiseC});
            combinePromise.Then(r =>
            {
                result = r;
            });
            
            promiseA.Complete(1);
            
            Assert.IsFalse(combinePromise.HasSucceeded);
            
            promiseB.Complete(2);
            
            Assert.IsFalse(combinePromise.HasSucceeded);
            
            promiseC.Complete(3);
            
            Assert.IsTrue(combinePromise.HasSucceeded);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(2, result[1]);
            Assert.AreEqual(3, result[2]);
        }
        
        [Test]
        public void CollectionVersionThrowsIfAnyPromisesThrow()
        {
            var promiseA = new Promise<int>();
            var promiseB = new Promise<int>();
            var promiseC = new Promise<int>();

            var combinePromise = Promise.Combine(new IPromise<int>[] {promiseA, promiseB, promiseC});
            
            promiseA.Complete(1);
            
            Assert.IsFalse(combinePromise.HasException);
            
            promiseB.Throw(new Exception());
            
            Assert.IsTrue(combinePromise.HasException);
            
            promiseC.Complete(3);
            
            Assert.IsTrue(combinePromise.HasException);
        }
    }
}
