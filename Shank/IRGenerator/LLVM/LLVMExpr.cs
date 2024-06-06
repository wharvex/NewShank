using LLVMSharp.Interop;
using Shank.ASTNodes;

namespace Shank.ExprVisitors;

public class LLVMExpr : ExpressionVisitor<LLVMValueRef>
{
    private Context _context { get; set; }
    private LLVMBuilderRef _builder { get; set; }
    private LLVMModuleRef _module { get; set; }

    public LLVMExpr(Context context, LLVMBuilderRef builder, LLVMModuleRef module)
    {
        _context = context;
        _builder = builder;
        _module = module;
    }

    private Types _types(LLVMTypeRef typeRef)
    {
        if (
            typeRef == LLVMTypeRef.Int64
            || typeRef == LLVMTypeRef.Int1
            || typeRef == LLVMTypeRef.Int8
        )
            return Types.INTEGER;
        else if (typeRef == LLVMTypeRef.Double)
            return Types.FLOAT;
        else if (typeRef == _context.StringType)
            return Types.STRING;
        else
            throw new Exception("undefined type");
    }

    public override LLVMValueRef Accept(IntNode node)
    {
        return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)node.Value);
    }

    public override LLVMValueRef Accept(FloatNode node)
    {
        return LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, node.Value);
    }

    public override LLVMValueRef Accept(VariableReferenceNode node)
    {
        LLVMValue value = _context.GetVaraible(node.Name);
        return _builder.BuildLoad2(value.TypeRef, value.ValueRef);
    }

    public override LLVMValueRef Accept(CharNode node)
    {
        return LLVMValueRef.CreateConstInt(_module.Context.Int8Type, (ulong)(node.Value));
    }

    public override LLVMValueRef Accept(BoolNode node)
    {
        return LLVMValueRef.CreateConstInt(_module.Context.Int1Type, (ulong)(node.GetValueAsInt()));
    }

    public override LLVMValueRef Accept(StringNode node)
    {
        // a string in llvm is just length + content
        var stringLength = LLVMValueRef.CreateConstInt(
            _module.Context.Int32Type,
            (ulong)node.Value.Length
        );

        var stringContent = _builder.BuildGlobalStringPtr(node.Value);

        // if we never mutate the string part directly, meaning when we do assignment we assign it a new string struct, then we do not need to do this malloc,
        // and we could just insert, the string constant in the string struct, we could do this because we don't directly mutate the string,
        // and the way we currently define string constants, they must not be mutated
        // one problem is that constant llvm strings are null terminated
        var stringPointer = _builder.BuildMalloc(
            LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)node.Value.Length)
        );
        _builder.BuildCall2(
            _context.CFuntions.memcpy.TypeOf,
            _context.CFuntions.memcpy.Function,
            [stringPointer, stringContent, stringLength]
        );
        var String = LLVMValueRef.CreateConstStruct(
            [
                LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
                LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
            ],
            false
        );
        String = _builder.BuildInsertValue(String, stringLength, 0);
        String = _builder.BuildInsertValue(String, stringPointer, 1);
        return String;
    }

    public override LLVMValueRef Accept(MathOpNode node)
    {
        LLVMValueRef L = node.Left.Visit(this);
        LLVMValueRef R = node.Right.Visit(this);
        if (_types(L.TypeOf) == Types.INTEGER)
        {
            return node.Op switch
            {
                MathOpNode.MathOpType.plus => _builder.BuildAdd(L, R, "addtmp"),
                MathOpNode.MathOpType.minus => _builder.BuildSub(L, R, "subtmp"),
                MathOpNode.MathOpType.times => _builder.BuildMul(L, R, "multmp"),
                MathOpNode.MathOpType.divide => _builder.BuildSDiv(L, R, "divtmp"),
                MathOpNode.MathOpType.modulo => _builder.BuildURem(L, R, "modtmp"),
                _ => throw new Exception("unsupported operation")
            };
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            return node.Op switch
            {
                MathOpNode.MathOpType.plus => _builder.BuildFAdd(L, R, "addtmp"),
                MathOpNode.MathOpType.minus => _builder.BuildFSub(L, R, "subtmp"),
                MathOpNode.MathOpType.times => _builder.BuildFMul(L, R, "multmp"),
                MathOpNode.MathOpType.divide => _builder.BuildFDiv(L, R, "divtmp"),
                _ => throw new Exception("unsupported operation")
            };
        }
        else if (_types(L.TypeOf) == Types.STRING)
        {
            if (node.Op == MathOpNode.MathOpType.plus)
            {
                // LLVMValueRef r = node.Right.Visit(this);
                // LLVMValueRef l = node.Left.Visit(this);
                var lSize = _builder.BuildExtractValue(L, 0);
                var rSize = _builder.BuildExtractValue(R, 0);
                var newSize = _builder.BuildAdd(lSize, rSize);
                // allocate enough (or perhaps in the future more than enough) space, for the concatenated string
                // I think this has to be heap allocated, because we can assign this to an out parameter of a function, and we would then lose it if it was stack allocated
                var newContent = _builder.BuildCall2(
                    _context.CFuntions.malloc.TypeOf,
                    _context.CFuntions.malloc.Function,
                    [newSize]
                );
                // fill the first part of the string
                _builder.BuildCall2(
                    _context.CFuntions.memcpy.TypeOf,
                    _context.CFuntions.memcpy.Function,
                    [newContent, _builder.BuildExtractValue(L, 1), lSize]
                );
                // fill the second part of the string
                // first "increment" the pointer of the string to be after the contents of the first part
                var secondPart = _builder.BuildInBoundsGEP2(LLVMTypeRef.Int8, newContent, [lSize]);
                _builder.BuildCall2(
                    _context.CFuntions.memcpy.TypeOf,
                    _context.CFuntions.memcpy.Function,
                    [secondPart, _builder.BuildExtractValue(R, 1), rSize]
                );
                var String = LLVMValueRef.CreateConstStruct(
                    [
                        LLVMValueRef.CreateConstNull(LLVMTypeRef.Int32),
                        LLVMValueRef.CreateConstNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0))
                    ],
                    false
                );
                String = _builder.BuildInsertValue(String, newSize, 0);
                String = _builder.BuildInsertValue(String, newContent, 1);
                return String;
            }
            else
            {
                throw new Exception(
                    $"strings can only be concatenated, you tried to {node.Op}, with strings"
                );
            }
        }
        else
        {
            throw new Exception("error");
        }
    }

    public override LLVMValueRef Accept(BooleanExpressionNode node)
    {
        LLVMValueRef L = node.Left.Visit(this);
        LLVMValueRef R = node.Right.Visit(this);

        if (_types(L.TypeOf) == Types.INTEGER)
        {
            return node.Op switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.eq
                    => _builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ne
                    => _builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.lt
                    => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.le
                    => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.gt
                    => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ge
                    => _builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, L, R, "cmp"),
                _ => throw new Exception("not accepted op")
            };
        }
        else if (_types(L.TypeOf) == Types.FLOAT)
        {
            return node.Op switch
            {
                BooleanExpressionNode.BooleanExpressionOpType.eq
                    => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ne
                    => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.lt
                    => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.le
                    => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.gt
                    => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, L, R, "cmp"),
                BooleanExpressionNode.BooleanExpressionOpType.ge
                    => _builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, L, R, "cmp"),

                _ => throw new Exception("")
            };
        }

        throw new Exception("undefined bool");
    }

    public override LLVMValueRef Accept(RecordNode node)
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef Accept(ParameterNode node)
    {
        return node.IsVariable
            ? _context.GetVaraible(node.Variable?.Name).ValueRef
            : node.Constant.Visit(this);
    }
}
