namespace Shank;

public class IteratorDataType : InterpreterDataType
{
    public IEnumerator<int> Enumerator;

    public IteratorDataType(int count)
    {
        Enumerator = Enumerable.Range(0, count).GetEnumerator();
    }
    public bool MoveNext()
    {
        return Enumerator.MoveNext();
    }
    
    public int Current
    {
        get { return Enumerator.Current; }
    }
    public override string ToString()
    {
        return "IteratorDataType";
    }

    public override void FromString(string input) { }
}
