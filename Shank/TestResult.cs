namespace Shank;

public class TestResult
{
    public string testName;
    public string parentFunctionName;
    public int lineNum;
    public LinkedList<AssertResult> Asserts = new LinkedList<AssertResult>();

    public TestResult(string testName, string parentFunctionName)
    {
        this.testName = testName;
        this.parentFunctionName = parentFunctionName;
    }

    public override string ToString()
    {
        return "Test "
            + testName
            + " (line: "
            + lineNum
            + " ) results:\n"
            + string.Join("\n", Asserts);
    }
}
