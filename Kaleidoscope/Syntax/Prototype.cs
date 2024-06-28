using System.Text;

namespace Kaleidoscope.Syntax;

/// <summary>
/// A function prototype.
/// </summary>
/// <param name="Name">The function's name.</param>
/// <param name="Params">The function's parameter names.</param>
public sealed record Prototype(string Name, List<string> Params) : IItem
{
    public override string ToString()
    {
        var str = new StringBuilder($"{Name}(");

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