using System;
using System.IO;

namespace Shank.Tran;

public class StringHandler
{
    private int index = 0;
    private string TranFile;

    public StringHandler(string input)
    {
        try
        {
            TranFile = input;
        }
        catch (IOException e)
        {
            throw new Exception("Error in reading the file: " + e.Message);
        }
    }

    public char Peek(int i)
    {
        int PeekIndex = index + i;
        if (PeekIndex >= 0 && PeekIndex < TranFile.Length)
        {
            return TranFile[PeekIndex];
        }
        return '\0';
    }

    public string PeekString(int i)
    {
        int PeekIndex = index + i;
        if (PeekIndex >= 0 && PeekIndex <= TranFile.Length)
        {
            return TranFile.Substring(index, PeekIndex - index);
        }
        return string.Empty;
    }

    public char GetChar()
    {
        if (index < TranFile.Length)
        {
            char current = TranFile[index];
            index++;
            return current;
        }
        return '\0';
    }

    public void Swallow(int i)
    {
        index += i;
    }

    public bool IsDone()
    {
        return index >= TranFile.Length;
    }

    public string Remainder()
    {
        return TranFile.Substring(index);
    }

    public string GetSubstring(int startPosition, int length)
    {
        if (startPosition >= 0 && (startPosition + length) <= TranFile.Length)
        {
            return TranFile.Substring(startPosition, length);
        }
        return string.Empty;
    }
}
