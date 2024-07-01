namespace Kaleidoscope.Syntax;

/// <summary>
/// A visitor for Kaleidoscope expressions.
/// </summary>
public interface IExprVisitor<out T>
{
    /// <summary>
    /// Visits a binary expression node.
    /// </summary>
    T Visit(BinaryExpr expr);

    /// <summary>
    /// Visits a call expression node.
    /// </summary>
    T Visit(CallExpr expr);

    /// <summary>
    /// Visits a number literal node.
    /// </summary>
    T Visit(NumberExpr expr);

    /// <summary>
    /// Visits a variable expression node.
    /// </summary>
    T Visit(VariableExpr expr);
}