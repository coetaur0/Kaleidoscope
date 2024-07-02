using Kaleidoscope.Codegen;
using Kaleidoscope.Parser;
using Kaleidoscope.Syntax;

var parser = new Parser();
var codegen = new LlvmCodegen();
while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null or "")
    {
        break;
    }

    try
    {
        var ast = parser.ParseItem(input);
        var ir = ast.Accept(codegen);
        Console.WriteLine(ir.ToString());

        switch (ast)
        {
            case Function { Prototype.Name: "__anon_expr" }:
                ir.DeleteFunction();
                break;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}

Console.WriteLine(codegen.Module.ToString());