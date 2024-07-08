using Kaleidoscope.Syntax;
using LLVMSharp.Interop;

namespace Kaleidoscope.Codegen;

/// <summary>
/// An LLVM code generator for the Kaleidoscope language. 
/// </summary>
public sealed class LLVMCodegen : IItemVisitor<LLVMValueRef>, IExprVisitor<LLVMValueRef>
{
    /// <summary>
    /// The LLVM module produced by codegen.
    /// </summary>
    public LLVMModuleRef Module { get; }

    private LLVMBuilderRef _builder;

    private LLVMPassManagerRef _passManager;

    private readonly Dictionary<string, LLVMValueRef> _variables;

    public LLVMCodegen()
    {
        Module = LLVMModuleRef.CreateWithName("kaleidoscope");
        _builder = Module.Context.CreateBuilder();
        _passManager = Module.CreateFunctionPassManager();
        _passManager.AddBasicAliasAnalysisPass();
        _passManager.AddPromoteMemoryToRegisterPass();
        _passManager.AddInstructionCombiningPass();
        _passManager.AddReassociatePass();
        _passManager.AddGVNPass();
        _passManager.AddCFGSimplificationPass();
        _passManager.InitializeFunctionPassManager();
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

        _passManager.RunFunctionPassManager(function);

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

    public LLVMValueRef Visit(IfExpr expr)
    {
        var cond = expr.Condition.Accept(this);
        var condValue = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, cond,
            LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 0.0), "ifcond");

        var function = _builder.InsertBlock.Parent;
        var thenBlock = function.AppendBasicBlock("then");
        var elseBlock = function.AppendBasicBlock("else");
        var mergeBlock = function.AppendBasicBlock("ifcont");
        _builder.BuildCondBr(condValue, thenBlock, elseBlock);

        _builder.PositionAtEnd(thenBlock);
        var thenValue = expr.Then.Accept(this);
        thenBlock = _builder.InsertBlock;

        _builder.PositionAtEnd(elseBlock);
        var elseValue = expr.Else.Accept(this);
        elseBlock = _builder.InsertBlock;

        _builder.PositionAtEnd(mergeBlock);
        var phiNode = _builder.BuildPhi(LLVMTypeRef.Double, "iftmp");
        phiNode.AddIncoming([thenValue], [thenBlock], 1u);
        phiNode.AddIncoming([elseValue], [elseBlock], 1u);

        _builder.PositionAtEnd(thenBlock);
        _builder.BuildBr(mergeBlock);

        _builder.PositionAtEnd(elseBlock);
        _builder.BuildBr(mergeBlock);

        _builder.PositionAtEnd(mergeBlock);
        return phiNode;
    }

    public LLVMValueRef Visit(ForExpr expr)
    {
        throw new NotImplementedException();
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