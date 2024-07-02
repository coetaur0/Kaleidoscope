using Kaleidoscope.Codegen;
using Kaleidoscope.Parser;
using Kaleidoscope.Syntax;
using LLVMSharp.Interop;

var parser = new Parser();
var codegen = new LlvmCodegen();

LLVM.LinkInMCJIT();
LLVM.InitializeNativeTarget();
var interpreter = codegen.Module.CreateInterpreter();

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
        Console.WriteLine($"LLVM IR:\n{ir.ToString()}");

        switch (ast)
        {
            case Function { Prototype.Name: "__anon_expr" }:
                var function = codegen.Module.GetNamedFunction("__anon_expr");
                var result = interpreter.RunFunction(ir, Array.Empty<LLVMGenericValueRef>());
                Console.WriteLine($"Result: {LLVMTypeRef.Double.GenericValueToFloat(result)}");
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