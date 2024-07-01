using Kaleidoscope.Codegen;
using Kaleidoscope.Parser;

try
{
    var parser = new Parser();
    var ast = parser.ParseItem("def f(x, y) x + y * 4");
    var codegen = new LlvmCodegen();
    ast.Accept(codegen);
    Console.WriteLine($"{codegen.Module.PrintToString()}");
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}