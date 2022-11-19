using System;
using System.Collections.Generic;

namespace Promises
{
    public class CombinePromise<T,U> : Promise<(T,U)>
    {
        public CombinePromise(
            IPromise<T> a,
            IPromise<U> b)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);

            a.Then(r1 =>
                b.Then(r2 =>
                    Complete((r1, r2))
                ));
        }

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
    
    public class CombinePromise<T,U,V> : Promise<(T,U,V)>
    {
        public CombinePromise(
            IPromise<T> a,
            IPromise<U> b,
            IPromise<V> c)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);

            a.Then(r1 =>
                b.Then(r2 =>
                    c.Then(r3 =>
                    Complete((r1, r2, r3))
                )));
        }

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
    
    public class CombinePromise<T,U,V,W> : Promise<(T,U,V,W)>
    {
        public CombinePromise(
            IPromise<T> a,
            IPromise<U> b,
            IPromise<V> c,
            IPromise<W> d)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);

            a.Then(r1 =>
                b.Then(r2 =>
                    c.Then(r3 =>
                        d.Then(r4 =>
                            Complete((r1, r2, r3, r4))
                    ))));
        }

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
    
    public class CombinePromise<T,U,V,W,X> : Promise<(T,U,V,W,X)>
    {
        public CombinePromise(
            IPromise<T> a,
            IPromise<U> b,
            IPromise<V> c,
            IPromise<W> d,
            IPromise<X> e)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);
            e.Catch(TryThrow);

            a.Then(r1 =>
                b.Then(r2 =>
                    c.Then(r3 =>
                        d.Then(r4 =>
                            e.Then(r5 =>
                            Complete((r1, r2, r3, r4, r5))
                        )))));
        }

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
    
    public class CombinePromise<T,U,V,W,X,Y> : Promise<(T,U,V,W,X,Y)>
    {
        public CombinePromise(
            IPromise<T> a,
            IPromise<U> b,
            IPromise<V> c,
            IPromise<W> d,
            IPromise<X> e,
            IPromise<Y> f)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);
            e.Catch(TryThrow);
            f.Catch(TryThrow);

            a.Then(r1 =>
                b.Then(r2 =>
                    c.Then(r3 =>
                        d.Then(r4 =>
                            e.Then(r5 =>
                                f.Then(r6 =>
                                Complete((r1, r2, r3, r4, r5, r6))
                            ))))));
        }

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
    
    public class CombinePromise<T,U,V,W,X,Y,Z> : Promise<(T,U,V,W,X,Y,Z)>
    {
        public CombinePromise(
            IPromise<T> a,
            IPromise<U> b,
            IPromise<V> c,
            IPromise<W> d,
            IPromise<X> e,
            IPromise<Y> f,
            IPromise<Z> g)
        {
            a.Catch(TryThrow);
            b.Catch(TryThrow);
            c.Catch(TryThrow);
            d.Catch(TryThrow);
            e.Catch(TryThrow);
            f.Catch(TryThrow);
            g.Catch(TryThrow);

            a.Then(r1 =>
                b.Then(r2 =>
                    c.Then(r3 =>
                        d.Then(r4 =>
                            e.Then(r5 =>
                                f.Then(r6 =>
                                    g.Then(r7 =>
                                    Complete((r1, r2, r3, r4, r5, r6, r7))
                                )))))));
        }

        private void TryThrow(Exception e)
        {
            if (IsPending)
            {
                Throw(e);
            }
        }
    }
    
    public class CombinePromise<T> : Promise<IList<T>>
    {
        int _successes = 0;

        public CombinePromise(params IPromise<T>[] promises) : this( (ICollection<IPromise<T>>) promises) { }
        
        public CombinePromise(ICollection<IPromise<T>> promises)
        {
            if (promises.Count == 0)
            {
                Complete(Array.Empty<T>());
                return;
            }

            var results = new T[promises.Count];
            var index = 0;
            foreach (var promise in promises)
            {
                var closureIndex = index;
                promise
                    .Then(result => {
                        results[closureIndex] = result;
                        _successes++;

                        if (_successes == results.Length)
                        {
                            Complete(new List<T>(results));
                        }
                    })
                    .Catch(TryThrow);
                index++;
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