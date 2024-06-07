namespace Shank;

public class AssertResult
{
    public string parentTestName;
    public string? comparedValues;
    public bool passed;
    public int lineNum;

    public AssertResult(string parentTestName, bool passed)
    {
        this.parentTestName = parentTestName;
        this.passed = passed;
    }

    public AssertResult(string parentTestName)
    {
        this.parentTestName = parentTestName;
    }

    public override string ToString()
    {
        return parentTestName
            + " assertIsEqual (line: "
            + lineNum
            + ") "
            + comparedValues
            + " : "
            + (passed ? "passed" : "failed");
    }
}
