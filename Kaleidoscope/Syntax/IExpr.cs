namespace Kaleidoscope.Syntax;

/// <summary>
/// A Kaleidoscope expression.
/// </summary>
public interface IExpr
{
    /// <summary>
    /// The expression's source range.
    /// </summary>
    public Range Range { get; }

    /// <summary>
    /// Accepts an expression visitor on the node.
    /// </summary>
    T Accept<T>(IExprVisitor<T> visitor);
}