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
            var paramName = item.Prototype.Params[i];
            param.Name = paramName;
            var alloca = _builder.BuildAlloca(LLVMTypeRef.Double, paramName);
            _builder.BuildStore(param, alloca);
            _variables[paramName] = alloca;
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
        var function = _builder.InsertBlock.Parent;
        var alloca = _builder.BuildAlloca(LLVMTypeRef.Double, expr.VarName);
        var start = expr.Start.Accept(this);
        _builder.BuildStore(start, alloca);

        var loopBlock = function.AppendBasicBlock("loop");
        _builder.BuildBr(loopBlock);
        _builder.PositionAtEnd(loopBlock);
        _variables.TryGetValue(expr.VarName, out var oldValue);
        _variables[expr.VarName] = alloca;
        expr.Body.Accept(this);

        var step = expr.Step.Accept(this);
        var endCond = expr.End.Accept(this);
        var currentVar = _builder.BuildLoad2(LLVMTypeRef.Double, alloca, expr.VarName);
        var nextVar = _builder.BuildFAdd(currentVar, step, "nextvar");
        _builder.BuildStore(nextVar, alloca);
        var zero = LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, 0);
        endCond = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, endCond, zero, "loopcond");

        var afterBlock = function.AppendBasicBlock("afterloop");
        _builder.BuildCondBr(endCond, loopBlock, afterBlock);
        _builder.PositionAtEnd(afterBlock);
        if (oldValue.Handle != IntPtr.Zero)
        {
            _variables[expr.VarName] = oldValue;
        }
        else
        {
            _variables.Remove(expr.VarName);
        }

        return zero;
    }

    public LLVMValueRef Visit(VarInExpr expr)
    {
        var initVal = expr.Value.Accept(this);
        var alloca = _builder.BuildAlloca(LLVMTypeRef.Double, expr.Name);
        _builder.BuildStore(initVal, alloca);
        _variables.TryGetValue(expr.Name, out var oldValue);
        _variables[expr.Name] = alloca;
        var body = expr.Body.Accept(this);
        if (oldValue.Handle != IntPtr.Zero)
        {
            _variables[expr.Name] = oldValue;
        }
        else
        {
            _variables.Remove(expr.Name);
        }

        return body;
    }

    public LLVMValueRef Visit(BinaryExpr expr)
    {
        if (expr.Op == "=")
        {
            if (expr.Lhs is not VariableExpr target)
            {
                throw Exception("destination of '=' must be a variable", expr.Lhs.Range);
            }

            var value = expr.Rhs.Accept(this);
            if (!_variables.TryGetValue(target.Name, out var variable))
            {
                throw Exception($"unknown variable {target.Name}", expr.Range);
            }

            _builder.BuildStore(value, variable);
            return value;
        }

        var lhsValue = expr.Lhs.Accept(this);
        var rhsValue = expr.Rhs.Accept(this);

        switch (expr.Op)
        {
            case "<":
                var result = _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, lhsValue, rhsValue, "cmptmp");
                return _builder.BuildUIToFP(result, LLVMTypeRef.Double, "booltmp");
            case "+":
                return _builder.BuildFAdd(lhsValue, rhsValue, "tmpadd");
            case "-":
                return _builder.BuildFSub(lhsValue, rhsValue, "tmpsub");
            case "*":
                return _builder.BuildFMul(lhsValue, rhsValue, "tmpmul");
            default:
                var function = Module.GetNamedFunction($"binary{expr.Op}");
                if (function.Handle == IntPtr.Zero)
                {
                    throw Exception("unknown operator referenced", expr.Range);
                }

                var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double,
                    Enumerable.Repeat(LLVMTypeRef.Double, 2).ToArray());
                return _builder.BuildCall2(functionType, function, new[] { lhsValue, rhsValue }, "binop");
        }
    }

    public LLVMValueRef Visit(UnaryExpr expr)
    {
        var operandValue = expr.Operand.Accept(this);
        var function = Module.GetNamedFunction($"unary{expr.Op}");
        if (function.Handle == IntPtr.Zero)
        {
            throw Exception("unknown operator referenced", expr.Range);
        }

        var functionType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Double, [LLVMTypeRef.Double]);
        return _builder.BuildCall2(functionType, function, new[] { operandValue }, "unop");
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

        return _builder.BuildLoad2(LLVMTypeRef.Double, value, expr.Name);
    }

    private static CodegenException Exception(string message, Syntax.Range range)
    {
        return new CodegenException($"Compilation error at {range}: {message}.");
    }
}