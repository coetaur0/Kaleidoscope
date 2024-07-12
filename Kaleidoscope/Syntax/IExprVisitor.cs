namespace Kaleidoscope.Syntax;

/// <summary>
/// A visitor for Kaleidoscope expressions.
/// </summary>
public interface IExprVisitor<out T>
{
    /// <summary>
    /// Visits a conditional expression node.
    /// </summary>
    T Visit(IfExpr expr);

    /// <summary>
    /// Visits a for loop expression node.
    /// </summary>
    T Visit(ForExpr expr);

    /// <summary>
    /// Visits a binary expression node.
    /// </summary>
    T Visit(BinaryExpr expr);

    /// <summary>
    /// Visits a unary expression node.
    /// </summary>
    T Visit(UnaryExpr expr);

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