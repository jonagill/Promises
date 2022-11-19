using System;
using System.Collections.Generic;

namespace Promises
{
    public class AllPromise : Promise
    {
        public AllPromise(
            IPromise a,
            IPromise b)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);

            a.Then(() =>
                b.Then(Complete));
        }
        
        public AllPromise(
            IPromise a,
            IPromise b,
            IPromise c)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);

            a.Then(() =>
                b.Then(() =>
                    c.Then(Complete)));
        }
        
        public AllPromise(
            IPromise a,
            IPromise b,
            IPromise c,
            IPromise d)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);

            a.Then(() =>
                b.Then(() =>
                    c.Then(() => 
                        d.Then(Complete))));
        }
        
        public AllPromise(
            IPromise a,
            IPromise b,
            IPromise c,
            IPromise d,
            IPromise e)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);
            e.Catch(TryThrow);

            a.Then(() =>
                b.Then(() =>
                    c.Then(() => 
                        d.Then(() =>
                            e.Then(Complete)))));
        }
        
        public AllPromise(
            IPromise a,
            IPromise b,
            IPromise c,
            IPromise d,
            IPromise e,
            IPromise f)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);
            e.Catch(TryThrow);
            f.Catch(TryThrow);

            a.Then(() =>
                b.Then(() =>
                    c.Then(() => 
                        d.Then(() =>
                            e.Then(() =>
                                f.Then(Complete))))));
        }
        
        public AllPromise(params IPromise[] promises) : this((ICollection<IPromise>) promises) {}

        public AllPromise(ICollection<IPromise> promises)
        {
            var count = promises.Count;
            var successes = 0;
            foreach (var promise in promises)
            {
                promise.Catch(TryThrow);
                promise.Then(() =>
                {
                    successes++;
                    if (successes == count)
                    {
                        Complete();
                    }
                });
            }
        }
        

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
}