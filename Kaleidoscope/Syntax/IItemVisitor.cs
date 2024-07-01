namespace Kaleidoscope.Syntax;

/// <summary>
/// A visitor for top-level items.
/// </summary>
public interface IItemVisitor<out T>
{
    /// <summary>
    /// Visits a function node.
    /// </summary>
    T Visit(Function item);

    /// <summary>
    /// Visits a prototype node.
    /// </summary>
    T Visit(Prototype item);
}