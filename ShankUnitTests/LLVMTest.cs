namespace ShankUnitTests;

[TestClass]
public class LLVMTest
{
    [TestMethod]
    public void Expression()
    {
        string[] code = { "define start()\n", "variables p : integer\n", "\tp:=3+1+2+3\n", };
    }
}
