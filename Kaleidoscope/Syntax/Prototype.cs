using System.Text;

namespace Kaleidoscope.Syntax;

/// <summary>
/// A function prototype.
/// </summary>
/// <param name="Name">The function's name.</param>
/// <param name="Params">The function's parameter names.</param>
/// <param name="IsOp">A boolean flag indicating if the function is an operator.</param>
/// <param name="Precedence">The function's precedence level, if it is an operator.</param>
/// <param name="Range">The prototype's source range.</param>
public sealed record Prototype(string Name, List<string> Params, bool IsOp, int Precedence, Range Range) : IItem
{
    public bool IsUnaryOp => IsOp && Params.Count == 1;

    public bool IsBinaryOp => IsOp && Params.Count == 2;

    public T Accept<T>(IItemVisitor<T> visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString()
    {
        var str = new StringBuilder($"{Name}");

        if (IsBinaryOp)
        {
            str.Append($" {Precedence} ");
        }

        str.Append('(');
        foreach (var param in Params)
        {
            str.Append($"{param}, ");
        }

        if (Params.Count != 0)
        {
            str.Remove(str.Length - 2, 2);
        }

        str.Append(')');
        return str.ToString();
    }
}