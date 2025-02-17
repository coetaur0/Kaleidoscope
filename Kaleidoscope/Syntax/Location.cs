namespace Kaleidoscope.Syntax;

/// <summary>
/// A location in a source.
/// </summary>
/// <param name="Line">The location's line number.</param>
/// <param name="Column">The location's column number.</param>
/// <param name="Offset">The location's offset from the source's start.</param>
public sealed record Location(int Line, int Column, int Offset)
{
    public override string ToString() => $"{Line}:{Column}";
}