using Kaleidoscope.Codegen;
using Kaleidoscope.Parser;
using Kaleidoscope.Syntax;
using LLVMSharp.Interop;

namespace Kaleidoscope.Interpreter;

/// <summary>
/// An interpreter for Kaleidoscope programs.
/// </summary>
public sealed class Interpreter
{
    private readonly Parser.Parser _parser;

    private readonly LLVMCodegen _codegen;

    private LLVMExecutionEngineRef _engine;

    /// <summary>
    /// Returns the LLVM module being built inside the interpreter. 
    /// </summary>
    public LLVMModuleRef Module => _codegen.Module;

    public Interpreter()
    {
        _parser = new Parser.Parser();
        _codegen = new LLVMCodegen();
        LLVM.LinkInMCJIT();
        LLVM.InitializeNativeTarget();
        _engine = _codegen.Module.CreateInterpreter();
    }

    /// <summary>
    /// Runs the interpreter on some input string and returns the LLVM IR produced for it, along with a result if the
    /// input is an expression. 
    /// </summary>
    public (string, double?) Run(string input)
    {
        try
        {
            var item = _parser.ParseItem(input);
            var ir = item.Accept(_codegen);
            var code = ir.ToString();
            double? result = null;
            switch (item)
            {
                case Function { Prototype.Name: "__anon_expr" }:
                    var value = _engine.RunFunction(ir, Array.Empty<LLVMGenericValueRef>());
                    result = LLVMTypeRef.Double.GenericValueToFloat(value);
                    ir.DeleteFunction();
                    break;
            }

            return (code, result);
        }
        catch (ParseException e)
        {
            throw new InterpreterException(e.Message);
        }
        catch (CodegenException e)
        {
            var function = Module.GetNamedFunction("__anon_expr");
            if (function.Handle != IntPtr.Zero)
            {
                function.DeleteFunction();
            }

            throw new InterpreterException(e.Message);
        }
    }
}