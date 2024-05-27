using LLVMSharp.Interop;
using Shank.ASTNodes;
using Shank.Interfaces;

namespace Shank.IRGenerator;

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
            default:
                throw new NotImplementedException();
        }
    }
}
