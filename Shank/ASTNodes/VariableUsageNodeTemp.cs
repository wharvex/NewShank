namespace Shank.ASTNodes;

public abstract class VariableUsageNodeTemp : ExpressionNode
{
    public VariableUsagePlainNode GetPlain()
    {
        var ret = this;
        while (ret is not VariableUsagePlainNode)
        {
            ret = ret is VariableUsageIndexNode vin
                ? vin.Left
                : ((VariableUsageMemberNode)ret).Left;
        }

        return (VariableUsagePlainNode)ret;
    }
}
