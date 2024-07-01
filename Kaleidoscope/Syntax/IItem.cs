namespace Kaleidoscope.Syntax;

/// <summary>
/// A top-level item.
/// </summary>
public interface IItem
{
    T Accept<T>(IItemVisitor<T> visitor);
    
    /// <summary>
    /// The item's source range.
    /// </summary>
    public Range Range { get; }
}