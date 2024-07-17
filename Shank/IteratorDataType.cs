namespace Shank;

public class IteratorDataType : InterpreterDataType
{
    public IEnumerator<int> Enumerator { get; private set; }

    public IteratorDataType(int count)
    {
        Enumerator = Enumerable.Range(0, count).GetEnumerator();
    }
    
    public override string ToString()
    {
        return "IteratorDataType";
    }

    public override void FromString(string input)
    {
       
    }
}