using System;
using System.Collections.Generic;

namespace Promises
{
    public class AnyPromise : Promise
    {
        private readonly int _maxThrows = 0;
        private int _throws = 0;
        
        public AnyPromise(
            IPromise a,
            IPromise b)
        {
            _maxThrows = 2;
            
            a.Then(TryComplete);
            a.Catch(exception => TryThrow());
            
            b.Then(TryComplete);
            b.Catch(exception => TryThrow());
        }
        
        public AnyPromise(
            IPromise a,
            IPromise b,
            IPromise c)
        {
            _maxThrows = 3;

            a.Then(TryComplete);
            a.Catch(exception => TryThrow());

            b.Then(TryComplete);
            b.Catch(exception => TryThrow());

            c.Then(TryComplete);
            c.Catch(exception => TryThrow());
        }
        
        public AnyPromise(
            IPromise a,
            IPromise b,
            IPromise c,
            IPromise d)
        {
            _maxThrows = 4;

            a.Then(TryComplete);
            a.Catch(exception => TryThrow());

            b.Then(TryComplete);
            b.Catch(exception => TryThrow());

            c.Then(TryComplete);
            c.Catch(exception => TryThrow());
            
            d.Then(TryComplete);
            d.Catch(exception => TryThrow());
        }
        
        public AnyPromise(
            IPromise a,
            IPromise b,
            IPromise c,
            IPromise d,
            IPromise e)
        {
            _maxThrows = 5;

            a.Then(TryComplete);
            a.Catch(exception => TryThrow());

            b.Then(TryComplete);
            b.Catch(exception => TryThrow());

            c.Then(TryComplete);
            c.Catch(exception => TryThrow());

            d.Then(TryComplete);
            d.Catch(exception => TryThrow());
            
            e.Then(TryComplete);
            e.Catch(exception => TryThrow());
        }
        
        public AnyPromise(
            IPromise a,
            IPromise b,
            IPromise c,
            IPromise d,
            IPromise e,
            IPromise f)
        {
            _maxThrows = 6;

            a.Then(TryComplete);
            a.Catch(exception => TryThrow());

            b.Then(TryComplete);
            b.Catch(exception => TryThrow());

            c.Then(TryComplete);
            c.Catch(exception => TryThrow());

            d.Then(TryComplete);
            d.Catch(exception => TryThrow());
            
            e.Then(TryComplete);
            e.Catch(exception => TryThrow());
            
            f.Then(TryComplete);
            f.Catch(exception => TryThrow());
        }
        
        public AnyPromise(params IPromise[] promises) : this((ICollection<IPromise>) promises) {}

        public AnyPromise(ICollection<IPromise> promises)
        {
            _maxThrows = promises.Count;
            foreach (var promise in promises)
            {
                promise.Then(TryComplete);
                promise.Catch(e => TryThrow());
            }
        }
        

        private void TryComplete()
        {
            if (IsPending)
            {
                Complete();
            }
        }

        private void TryThrow()
        {
            _throws++;
            if (_throws == _maxThrows)
            {
                Throw(new Exception("No promise completed successfully."));
            }
        }
    }
}