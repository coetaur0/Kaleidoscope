namespace Kaleidoscope.Parser;

/// <summary>
/// A source of code.
/// </summary>
internal sealed class Source(string? path, string contents)
{
    /// <summary>
    /// The path to the source, if it was loaded from a file.
    /// </summary>
    public string? Path { get; } = path;

    /// <summary>
    /// Returns the character at some offset in the source.
    /// </summary>
    public char this[int offset] => contents[offset];

    /// <summary>
    /// Returns the contents covered by some range in the source.
    /// </summary>
    public string this[Range range] => this[range.Start.Offset, range.End.Offset].ToString();

    /// <summary>
    /// Returns the source's length.
    /// </summary>
    public int Length => contents.Length;

    internal ReadOnlySpan<char> this[int start, int end]
    {
        get
        {
            var offset = Math.Max(0, Math.Min(start, contents.Length));
            var length = Math.Min(end, contents.Length) - offset;
            return contents.AsSpan().Slice(offset, length);
        }
    }
}