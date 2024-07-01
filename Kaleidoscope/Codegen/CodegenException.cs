namespace Kaleidoscope.Codegen;

/// <summary>
/// An exception thrown during code generation if an error is encountered.
/// </summary>
public class CodegenException : Exception
{
    public CodegenException()
    {
    }

    public CodegenException(string message) : base(message)
    {
    }

    public CodegenException(string message, Exception inner) : base(message, inner)
    {
    }
}