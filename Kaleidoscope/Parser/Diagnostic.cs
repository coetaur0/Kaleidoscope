namespace Kaleidoscope.Parser;

/// <summary>
/// A diagnostic emitted by the parser when it encounters syntax errors.
/// </summary>
/// <param name="Message">The diagnostic's message.</param>
/// <param name="Range">The diagnostic's source range.</param>
internal readonly record struct Diagnostic(string Message, Range Range)
{
    public override string ToString() => $"{Range}: {Message}";
}