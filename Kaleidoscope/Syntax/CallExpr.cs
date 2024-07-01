using System.Text;

namespace Kaleidoscope.Syntax;

/// <summary>
/// A call expression.
/// </summary>
/// <param name="Callee">The expression's callee.</param>
/// <param name="Args">The expression's arguments.</param>
/// <param name="Range">The expression's source range.</param>
public sealed record CallExpr(string Callee, List<IExpr> Args, Range Range) : IExpr
{
    public override string ToString()
    {
        var str = new StringBuilder($"{Callee}(");

        foreach (var expr in Args)
        {
            str.Append($"{expr}, ");
        }

        if (Args.Count != 0)
        {
            str.Remove(str.Length - 2, 2);
        }

        str.Append(')');
        return str.ToString();
    }
}