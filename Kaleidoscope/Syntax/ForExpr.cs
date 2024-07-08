namespace Kaleidoscope.Syntax;

/// <summary>
/// A for loop expression.
/// </summary>
/// <param name="VarName">The name of the loop iteration variable.</param>
/// <param name="Start">The loop's iterations start.</param>
/// <param name="End">The loop's iteration end.</param>
/// <param name="Step">The loop's iteration step.</param>
/// <param name="Body">The loop's body.</param>
/// <param name="Range">The expression's source range.</param>
public sealed record ForExpr(string VarName, IExpr Start, IExpr End, IExpr Step, IExpr Body, Range Range) : IExpr
{
    public T Accept<T>(IExprVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString() => $"for {VarName} = {Start}, {End}, {Step} in {Body}";
}