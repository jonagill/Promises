using System;

/// <summary>
/// Represents an exception that was thrown during the execution of a promise's
/// Complete() or Throw() function.
/// </summary>
public class PromiseExecutionException : Exception
{
    public PromiseExecutionException(Exception innerException) : base(FormatMessage(innerException), innerException)
    {
    }

    // Provide the stacktrace of the inner exception so that consoles and debuggers
    // display the most relevant information.
    public override string StackTrace => InnerException.StackTrace;

    private static string FormatMessage(Exception innerException)
    {
        return $"{innerException.GetType().Name}: {innerException.Message}";
    }
}
