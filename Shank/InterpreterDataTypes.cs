﻿using System.Collections;

namespace Shank;

public abstract class InterpreterDataType
{
    public abstract override string ToString();
    public abstract void FromString(string input);
}

public class IntDataType : InterpreterDataType
{
    public IntDataType(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = int.Parse(input);
    }
}

public class FloatDataType : InterpreterDataType
{
    public FloatDataType(float value)
    {
        Value = value;
    }

    public float Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = float.Parse(input);
    }
}

public class BooleanDataType : InterpreterDataType
{
    public BooleanDataType(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = bool.Parse(input);
    }
}

public class StringDataType : InterpreterDataType
{
    public StringDataType(string value)
    {
        Value = value;
    }

    public string Value { get; set; }

    public override string ToString()
    {
        return Value;
    }

    public override void FromString(string input)
    {
        Value = input;
    }
}

public class CharDataType : InterpreterDataType
{
    public CharDataType(char value)
    {
        Value = value;
    }

    public char Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }

    public override void FromString(string input)
    {
        Value = input[0];
    }
}

public class ArrayDataType : InterpreterDataType
{
    public List<object> Value { get; }

    // TODO: Is this bad form since DataType is part of an AST node (VariableNode), and
    // ArrayDataType is part of the Interpreter?
    public VariableNode.DataType ArrayContentsType { get; init; }

    // We don't need 'from' or 'to' because Semantic Analysis should take care of error-checking
    // issues with these.
    public ArrayDataType(VariableNode.DataType arrayContentsType)
    {
        Value = [];
        ArrayContentsType = arrayContentsType;
    }

    public ArrayDataType(List<object> val, VariableNode.DataType arrayContentsType)
    {
        Value = val;
        ArrayContentsType = arrayContentsType;
    }

    public void AddElement(object element, int idx)
    {
        Value.Insert(idx, element);
    }

    public object GetElement(int idx)
    {
        var ret =
            Value[idx]
            ?? throw new InvalidOperationException(
                "Something went wrong internally. No element of this List--which models a "
                    + "Shank array--should be null, because there is no such thing as null in Shank"
            );
        return ret;
    }

    public float GetElementReal(int idx)
    {
        return (float)GetElement(idx);
    }

    public int GetElementInteger(int idx)
    {
        return (int)GetElement(idx);
    }

    public string GetElementString(int idx)
    {
        return (string)GetElement(idx);
    }

    public char GetElementCharacter(int idx)
    {
        if (GetElement(idx) is char c)
        {
            return c;
        }

        throw new InvalidOperationException("Expected array element to be of type: character");
    }

    public bool GetElementBoolean(int idx)
    {
        return (bool)GetElement(idx);
    }

    public override string ToString()
    {
        return "";
    }

    public override void FromString(string input) { }
}

public class RecordDataType : InterpreterDataType
{
    public Dictionary<string, object> Value { get; } = [];

    public Dictionary<string, VariableNode.DataType> MemberTypes { get; } = [];

    public RecordDataType(List<StatementNode> members)
    {
        members.ForEach(s =>
        {
            if (s is RecordMemberNode rmn)
            {
                MemberTypes[rmn.Name] = rmn.Type;
            }
            else
            {
                throw new InvalidOperationException(
                    "A RecordDataType must be initialized with a List of RecordMemberNode."
                );
            }
        });
    }

    public string GetValueString(string key)
    {
        return (string)Value[key];
    }

    public char GetValueCharacter(string key)
    {
        return (char)Value[key];
    }

    public int GetValueInteger(string key)
    {
        return (int)Value[key];
    }

    public float GetValueReal(string key)
    {
        return (float)Value[key];
    }

    public bool GetValueBoolean(string key)
    {
        return (bool)Value[key];
    }

    public override string ToString()
    {
        return "";
    }

    public override void FromString(string input) { }
}
