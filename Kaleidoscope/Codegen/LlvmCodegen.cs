using Kaleidoscope.Syntax;
using LLVMSharp.Interop;

namespace Kaleidoscope.Codegen;

/// <summary>
/// An LLVM code generator for the Kaleidoscope language. 
/// </summary>
public sealed class LlvmCodegen : IItemVisitor<LLVMValueRef>, IExprVisitor<LLVMValueRef>
{
    /// <summary>
    /// The LLVM module produced by codegen.
    /// </summary>
    public LLVMModuleRef Module { get; }

    private LLVMBuilderRef _builder;

    private readonly Dictionary<string, LLVMValueRef> _variables;

    public LlvmCodegen()
    {
        Module = LLVMModuleRef.CreateWithName("kaleidoscope");
        _builder = Module.Context.CreateBuilder();
        _variables = new Dictionary<string, LLVMValueRef>();
    }

    public LLVMValueRef Visit(Function item)
    {
        var function = Module.GetNamedFunction(item.Prototype.Name);

        if (function.Handle == IntPtr.Zero)
        {
            function = item.Prototype.Accept(this);
        }

        if (function.BasicBlocksCount != 0)
        {
            throw Exception($"invalid redefinition of function {item.Prototype.Name}", item.Range);
        }

        var block = function.AppendBasicBlock("entry");
        _builder.PositionAtEnd(block);

        _variables.Clear();
        for (var i = 0; i < item.Prototype.Params.Count; i++)
        {
            var param = function.GetParam((uint)i);
            _variables[item.Prototype.Params[i]] = param;
        }

        var returnValue = item.Body.Accept(this);
        _builder.BuildRet(returnValue);

        function.VerifyFunction(LLVMVerifierFailureAction.LLVMPrintMessageAction);

        return function;
    }

    public LLVMValueRef Visit(Prototype item)
    {
        var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double,
            Enumerable.Repeat(LLVMTypeRef.Double, item.Params.Count).ToArray());

        var function = Module.AddFunction(item.Name, functionType);
        function.Linkage = LLVMLinkage.LLVMExternalLinkage;

        for (var i = 0; i < item.Params.Count; i++)
        {
            var param = function.GetParam((uint)i);
            param.Name = item.Params[i];
        }

        return function;
    }

    public LLVMValueRef Visit(BinaryExpr expr)
    {
        var lhsValue = expr.Lhs.Accept(this);
        var rhsValue = expr.Rhs.Accept(this);

        switch (expr.Op)
        {
            case BinaryOp.LessThan:
                var result = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhsValue, rhsValue, "cmptmp");
                return _builder.BuildUIToFP(result, LLVMTypeRef.Double, "booltmp");
            case BinaryOp.Add:
                return _builder.BuildFAdd(lhsValue, rhsValue, "tmpadd");
            case BinaryOp.Subtract:
                return _builder.BuildFSub(lhsValue, rhsValue, "tmpsub");
            case BinaryOp.Multiply:
                return _builder.BuildFMul(lhsValue, rhsValue, "tmpmul");
            default:
                throw Exception("unknown operator", expr.Range);
        }
    }

    public LLVMValueRef Visit(CallExpr expr)
    {
        var function = Module.GetNamedFunction(expr.Callee);

        if (function.Handle == IntPtr.Zero)
        {
            throw Exception("unknown function referenced", expr.Range);
        }

        if (function.Params.Length != expr.Args.Count)
        {
            throw Exception("incorrect number of arguments passed", expr.Range);
        }

        var argValues = expr.Args.Select(arg => arg.Accept(this)).ToArray();
        var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double,
            Enumerable.Repeat(LLVMTypeRef.Double, expr.Args.Count).ToArray());

        return _builder.BuildCall2(functionType, function, argValues, "calltmp");
    }

    public LLVMValueRef Visit(NumberExpr expr)
    {
        return LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, expr.Value);
    }

    public LLVMValueRef Visit(VariableExpr expr)
    {
        if (!_variables.TryGetValue(expr.Name, out var value))
        {
            throw Exception($"unknown variable {expr.Name}", expr.Range);
        }

        return value;
    }

    private static CodegenException Exception(string message, Syntax.Range range)
    {
        return new CodegenException($"Compilation error at {range}: {message}.");
    }
}