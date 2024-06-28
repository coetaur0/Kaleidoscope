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
    public string this[Range range] => this[range.Start.Offset, range.End.Offset].ToString();

    /// <summary>
    /// Returns the source's length.
    /// </summary>
    public int Length => Contents.Length;

    internal string this[int start, int end]
    {
        get
        {
            var offset = Math.Max(0, Math.Min(start, Contents.Length));
            var length = Math.Min(end, Contents.Length) - offset;
            return Contents.Substring(offset, length);
        }
    }
}