# Promises
This library provides a C# implementation of the [promise](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise) pattern for use in Unity projects. Promises provide an easy to use and chainable alternative to traditional asynchronous function callbacks.


## Installation
We recommend you install the Promises library via [OpenUPM](https://openupm.com/packages/com.jonagill.promises/). Per OpenUPM's documentation:

1. Open `Edit/Project Settings/Package Manager`
2. Add a new Scoped Registry (or edit the existing OpenUPM entry) to read:
    * Name: `package.openupm.com`
    * URL: `https://package.openupm.com`
    * Scope(s): `com.jonagill.promises`
3. Click Save (or Apply)
4. Open Window/Package Manager
5. Click the + button
6. Select Add package by name...
6. Click Add

# Basic functionality
Promises provide a simple for returning asynchronous result and error callbacks from your functions.

For instance, you might declare a function like this:


```
public IPromise PromptForUserInput() 
{
    var promise = new Promise();    
    
    var dialog = UISystem.ShowOkCancelDialog("Are you there?");
    dialog.OkPressed += promise.Complete;
    dialog.CancelPressed += promise.Throw(new Exception("Canceled"));

    return promise;
}
```

Exteral code can then invoke your function and listen to results like this:

```
PromptForUserInput()
    .Then(() => Debug.Log("Promise completed!"))
    .Catch(exception => Debug.LogException(exception));
```

## Basic callbacks
`Then()` callbacks are executed when the Promise is completed (or immediately, if you subscribe to an already-completed promise). `Catch()` callbacks are completed when someone calls `Throw()` on the Promise. `Throw()` uses Exceptions to provide a strongly typed error-handling mechanism, but it does not actually throw the exception using C#'s `throw` keyword at any time.

## IPromise
Generally you want to return an `IPromise` from your functions rather than a `Promise`, as `IPromise` does not allow access to the `Complete()` and `Throw()` functions. By returning an `IPromise`, you make sure that anyone calling your code can subcribe to callbacks but is not able to alter the state of the Promise themselves.

## Returning results
While `Promise` is useful for operations that can only pass or fail, you will often want to pass a return value to any subscribers to your Promises. For that, you should use the `Promise<T>` class. This functions very much like the untyped `Promise`, but it requires a result parameter be passed to `Complete()`.

For instance, you might declare a function like this:

```
public IPromise PromptForTextEntry(string message) 
{
    var promise = new Promise();    
    
    var dialog = UISystem.ShowTextEntryDialog(message);
    dialog.ReturnPressed += promise.Complete(dialog.text);
    dialog.CancelPressed += promise.Throw(new Exception("Canceled"));

    return promise;
}
```

Exteral code can then invoke your function and listen to results like this:

```
PromptForTextEntry("What is your name?")
    .Then(text => 
    {
        GameManager.SetPlayerName(text);
        Debug.Log($"Text entered: {text}"); 
    })
    .Catch(exception => Debug.LogException(exception));
```

# Transforming results with Transform()
Sometimes a function does not return the exact data that you want. For this case, you can use the `Transform()` function to run some post-processing on the result provided by a Promise.

For instance, we could use `Transform()` to write this function that converts an `IPromise<string>` to an `IPromise<int>`:

```
public IPromise<int> GetPlayerAge() 
{
    return PromptForTextEntry("What is your age?")
        .Transform(text => 
        {
            if (int.TryParse(text, out int age)) 
            {
                return age;
            }

            return -1;
        });
}

```

A `TransformException()` function similarly exists for transforming one exception type to another.

# Chaining with ContinueWith()
In addition to synchronously transforming a result with `Transform()`, you can chain multiple asynchronous Promises together using the `ContinueWith()` function. This returns a promise representing the completion of the entire chain of Promises.

Each chained function is kicked off in sequence when the previous function's Promise completes. However, if a single function's Promise throws an exception, the remaining functions will not be invoked, and the chain promise returned by `ContinueWith()` will throw the exception as well.

For instance, you could write code like:

```
public IPromise<string> GetProfileImageUrl(string userId);
public IPromise<byte[]> DownloadImage(string url);
public IPromise<Texture2D> DecodeTexture(byte[] bytes)

public IPromise<Texture2D> GetLocalUserProfileImage() 
{
    return GetProfileImageUrl(localPlayer.userId)
        .ContinueWith(DownloadImage)
        .ContinueWith(DecodeTexture);
}
```

# Cancelable Promises
Sometimes you want to be able to cancel an asynchronous operation before it completes. For instance, if your user navigates away from a webpage before you finish downloading it, you don't have any reason to continue that download. For cases like this, there are the `CancelablePromise` and `CancelablePromise<T>` classes.

These classes provide a `Cancel()` function that cancels the promise, along with a  a corresponding `Canceled()` function that can be used to assign callbacks that will be fired if the Promise gets canceled. Calling `Cancel()` will set an `IsCanceled` flag on the Promise that your asynchronous code can check when seeing if it should continue execution or not.

Internally, CancelablePromises are constructing using the same [CancellationTokenSource](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource?view=net-7.0) class as C#'s Task-based asynchronous programming pattern. If you want to tie multiple CancelablePromises together (so that cancelling one cancels both), you can manually create a `CancellationTokenSource` and pass it to both CancelablePromises' constructors.

If you chain multiple CancelablePromises together with `ContinueWith()`, then cancelling one promise will automatically cancel the rest of the chain.

# Additional helpers
Some bonus helper methods are provided to make writing Promise-based code easier:

* `Promise.CompletedPromise` returns an untyped, already-completed `Promise`, which is handy for cases where you want a function to synchronously return without allocating a new promise.

* `Promise.FromResult<T>()` returns an already-completed `Promise<T>` with the given result.

* `Promise.FromException()` and `Promise.FromException<T>()` return promises that have already thrown the provided exception.

* `Promise.All()` returns a Promise that completes if all of the promises provided as arguments complete and throws an exception if any of the sub-promises throw an exception.

* `Promise.Any()` returns a Promise that completes as soon as any of the promises provided as arguments completes. It only throws an exception if every sub-promise throws an exception.

* `Promise.Combine()` returns a Promise that completes if all of the promises provided as arguments complete (much like `Promise.All()`), but it returns a tuple containing the combined results of all of the sub-promises.

# Async and tasks
In many ways, Promises are less robust than C#'s [built-in Task-based asynchronous functions](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/). However, it can be harder to learn to use Tasks correctly, and Unity's support for async methods was historically fairly unreliable (although it has become much better supported in the last couple of years).

If you do want to use Promises and Tasks together, this library provides simple extension methods for converting Promises to Tasks (`promise.AsTask()`) and vice versa (`task.AsPromise()`).

