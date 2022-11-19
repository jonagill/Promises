using System;
using System.Threading;
using System.Threading.Tasks;

namespace Promises
{
    public static class TaskHelpers
    {
        public static Task AsTask(this IPromise promise)
        {
            var tcs = new TaskCompletionSource<bool>();

            promise.Then(() => tcs.TrySetResult(false));
            promise.Catch(e => tcs.TrySetException(e));

            if (promise is IReadOnlyCancelablePromise cancelablePromise)
            {
                cancelablePromise.Canceled(() => tcs.TrySetCanceled());
            }

            return tcs.Task;
        }
        
        public static Task<TResult> AsTask<TResult>(this IPromise<TResult> promise)
        {
            var tcs = new TaskCompletionSource<TResult>();

            promise.Then(result => tcs.TrySetResult(result));
            promise.Catch(e => tcs.TrySetException(e));

            if (promise is IReadOnlyCancelablePromise cancelablePromise)
            {
                cancelablePromise.Canceled(() => tcs.TrySetCanceled());
            }

            return tcs.Task;
        }

        public static IPromise AsPromise(this Task task)
        {
            var promise = new Promise();

            void HandleTaskCompletion(Task t)
            {
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        promise.Throw(new Exception($"Promise source task {task} was canceled."));
                        break;
                    case TaskStatus.Faulted:
                        promise.Throw(t.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                    default:
                        promise.Complete();
                        break;
                }
            }

            if (task.IsCompleted)
            {
                HandleTaskCompletion(task);
            }
            else
            {
                task.ContinueWith(HandleTaskCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return promise;
        }
        
        public static IPromise<T> AsPromise<T>(this Task<T> task)
        {
            var promise = new Promise<T>();

            void HandleTaskCompletion(Task<T> t)
            {
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        promise.Throw(new Exception($"Promise source task {task} was canceled."));
                        break;
                    case TaskStatus.Faulted:
                        promise.Throw(t.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                    default:
                        promise.Complete(t.Result);
                        break;
                }
            }

            if (task.IsCompleted)
            {
                HandleTaskCompletion(task);
            }
            else
            {
                task.ContinueWith(HandleTaskCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return promise;
        }
        
        public static ICancelablePromise AsCancelablePromise(this Task task, CancellationToken token)
        {
            var promise = new CancelablePromise();

            void HandleTaskCompletion(Task t)
            {
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        promise.Cancel();
                        break;
                    case TaskStatus.Faulted:
                        promise.Throw(t.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                    default:
                        promise.Complete();
                        break;
                }
            }

            if (task.IsCompleted)
            {
                HandleTaskCompletion(task);
            }
            else
            {
                task.ContinueWith(HandleTaskCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return promise;
        }
        
        public static IPromise<T> AsCancelablePromise<T>(this Task<T> task)
        {
            var promise = new CancelablePromise<T>();

            void HandleTaskCompletion(Task<T> t)
            {
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        promise.Cancel();
                        break;
                    case TaskStatus.Faulted:
                        promise.Throw(t.Exception);
                        break;
                    case TaskStatus.RanToCompletion:
                    default:
                        promise.Complete(t.Result);
                        break;
                }
            }

            if (task.IsCompleted)
            {
                HandleTaskCompletion(task);
            }
            else
            {
                task.ContinueWith(HandleTaskCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return promise;
        }
    }
}