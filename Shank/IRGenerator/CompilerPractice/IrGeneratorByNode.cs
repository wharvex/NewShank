using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.Interfaces;

namespace Shank.IRGenerator.CompilerPractice;

public static class IrGeneratorByNode
{
    public static LLVMValueRef CreateValueRef(IrGenerator irGen, ASTNode node)
    {
        switch (node)
        {
            case FunctionCallNode funcCallNode:
            {
                return irGen.LlvmBuilder.BuildCall2(
                    irGen.PrintfFuncType,
                    irGen.LlvmModule.GetNamedFunction(
                        ((ILlvmTranslatable)funcCallNode).GetNameForLlvm()
                    ),
                    funcCallNode.Parameters.Select(pn => CreateValueRef(irGen, pn)).ToArray()
                );
            }
            case ParameterNode paramNode:
                if (paramNode.ValueIsStoredInVariable())
                {
                    throw new NotImplementedException();
                }
                return CreateValueRef(irGen, paramNode.GetConstantSafe());
            case StringNode stringNode:
                return irGen.LlvmBuilder.BuildGlobalStringPtr(stringNode.Value + "\n");
            case IntNode intNode:
                return irGen.LlvmBuilder.BuildGlobalStringPtr(intNode.Value + "\n");
            default:
                throw new NotImplementedException();
        }
    }

    public static LLVMTypeRef CreateTypeRefFromShankNode(IrGenerator irGen, ASTNode node)
    {
        switch (node)
        {
            case VariableNode vn:
                return CreateTypeRefFromShankType(irGen.LlvmContext, vn.Type);
            default:
                throw new NotImplementedException();
        }
    }

    private static LLVMTypeRef CreateTypeRefFromShankType(
        LLVMContextRef llvmContext,
        IType dataType
    ) =>
        dataType switch
        {
            IntegerType => llvmContext.Int64Type,
            RealType => llvmContext.DoubleType,
            StringType => LLVMTypeRef.CreatePointer(llvmContext.Int8Type, 0),
            BooleanType => llvmContext.Int1Type,
            CharacterType => llvmContext.Int8Type,
            _ => throw new NotImplementedException()
        };
}
