namespace Kaleidoscope.Parser;

/// <summary>
/// A source of code.
/// </summary>
internal sealed class Source(string contents)
{
    /// <summary>
    /// The source's contents.
    /// </summary>
    public string Contents { get; set; } = contents;

    /// <summary>
    /// Returns the character at some offset in the source.
    /// </summary>
    public char this[int offset] => Contents[offset];

    /// <summary>
    /// Returns the contents covered by some range in the source.
    /// </summary>
    public string this[Syntax.Range range] => this[range.Start.Offset, range.End.Offset];

    /// <summary>
    /// Returns the contents of the source between two bounds.
    /// </summary>
    public string this[int start, int end]
    {
        get
        {
            var offset = Math.Max(0, Math.Min(start, Contents.Length));
            var length = Math.Min(end, Contents.Length) - offset;
            return Contents.Substring(offset, length);
        }
    }

    /// <summary>
    /// Returns the source's length.
    /// </summary>
    public int Length => Contents.Length;
}