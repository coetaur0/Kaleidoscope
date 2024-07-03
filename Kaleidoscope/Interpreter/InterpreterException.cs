namespace Kaleidoscope.Interpreter;

/// <summary>
/// An exception thrown by the interpreter when an error is encountered.
/// </summary>
public sealed class InterpreterException : Exception
{
    public InterpreterException()
    {
    }

    public InterpreterException(string message) : base(message)
    {
    }

    public InterpreterException(string message, Exception inner) : base(message, inner)
    {
    }
}