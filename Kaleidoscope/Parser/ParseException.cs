namespace Kaleidoscope.Parser;

/// <summary>
/// An exception thrown by the parser when it encounters syntax errors in a source.
/// </summary>
public sealed class ParseException : Exception
{
    public ParseException()
    {
    }

    public ParseException(string message) : base(message)
    {
    }

    public ParseException(string message, Exception inner) : base(message, inner)
    {
    }
}