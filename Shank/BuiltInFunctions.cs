using Shank.ASTNodes;

namespace Shank;

public class BuiltInFunctions
{
    public static int numberOfBuiltInFunctions = 0;

    public static void Register(Dictionary<string, CallableNode> functionList)
    {
        // Note to the reader - this implementation is different than what I have the students writing in Java.
        // The concepts are the same, but this is more language appropriate. This would be too hard for
        // the students to do in Java.
        var retVal = new List<BuiltInFunctionNode>
        {
            new BuiltInVariadicFunctionNode("write", Write),
            MakeNode(
                "read",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "input",
                        Type = new StringType(),
                        IsConstant = false
                    }
                ],
                Read
            ),
            new BuiltInVariadicFunctionNode("writeToTest", WriteToTest),
            MakeNode(
                "squareRoot",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new RealType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new RealType(),
                        IsConstant = false
                    }
                ],
                SquareRoot
            ),
            MakeNode(
                "getRandom",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new IntegerType(),
                        IsConstant = false
                    }
                ],
                Random
            ),
            MakeNode(
                "integerToReal",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new IntegerType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new RealType(),
                        IsConstant = false
                    }
                ],
                IntegerToReal
            ),
            MakeNode(
                "realToInteger",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new RealType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new IntegerType(),
                        IsConstant = false
                    }
                ],
                RealToInteger
            ),
            MakeNode(
                "left",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new StringType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "amount",
                        Type = new IntegerType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new StringType(),
                        IsConstant = false
                    }
                ],
                Left
            ),
            MakeNode(
                "right",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new StringType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "amount",
                        Type = new IntegerType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new StringType(),
                        IsConstant = false
                    }
                ],
                Right
            ),
            MakeNode(
                "substring",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new StringType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "index",
                        Type = new IntegerType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "amount",
                        Type = new IntegerType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "result",
                        Type = new StringType(),
                        IsConstant = false
                    }
                ],
                Substring
            ),
            MakeNode(
                "assertIsEqual",
                [
                    new VariableDeclarationNode()
                    {
                        Name = "actualValue",
                        Type = new IntegerType(),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "targetValue",
                        Type = new IntegerType(),
                        IsConstant = true
                    }
                ],
                AssertIsEqual
            ),
            new("low", Low)
            {
                Line = 0,
                ParameterVariables =
                [
                    new VariableDeclarationNode()
                    {
                        Name = "someArray",
                        Type = new ArrayType(
                            new GenericType(
                                "T",
                                new FunctionGenericContext(
                                    "low",
                                    BuiltInFunctionNode.BuiltinModuleName,
                                    new TypeIndex([])
                                )
                            ),
                            ArrayType.DefaultRange
                        ),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "low",
                        Type = new IntegerType(),
                        IsConstant = false
                    }
                ],
                GenericTypeParameterNames = ["T"],
            },
            new("high", High)
            {
                Line = 0,
                ParameterVariables =
                [
                    new VariableDeclarationNode()
                    {
                        Name = "someArray",
                        Type = new ArrayType(
                            new GenericType(
                                "T",
                                new FunctionGenericContext(
                                    "high",
                                    BuiltInFunctionNode.BuiltinModuleName,
                                    new TypeIndex([])
                                )
                            ),
                            ArrayType.DefaultRange
                        ),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "high",
                        Type = new IntegerType(),
                        IsConstant = false
                    }
                ],
                GenericTypeParameterNames = ["T"],
            },
            new("allocateMemory", AllocateMemory)
            {
                Line = 0,
                ParameterVariables =
                [
                    new VariableDeclarationNode()
                    {
                        Name = "refersTo",
                        Type = new ReferenceType(
                            new GenericType(
                                "R",
                                new FunctionGenericContext(
                                    "allocateMemory",
                                    BuiltInFunctionNode.BuiltinModuleName,
                                    new TypeIndex([])
                                )
                            )
                        ),
                        IsConstant = false
                    }
                ],
                GenericTypeParameterNames = ["R"],
            },
            new("freeMemory", FreeMemory)
            {
                Line = 0,
                ParameterVariables =
                [
                    new VariableDeclarationNode()
                    {
                        Name = "refersTo",
                        Type = new ReferenceType(
                            new GenericType(
                                "R",
                                new FunctionGenericContext(
                                    "freeMemory",
                                    BuiltInFunctionNode.BuiltinModuleName,
                                    new TypeIndex([])
                                )
                            )
                        ),
                        IsConstant = false
                    }
                ],
                GenericTypeParameterNames = ["R"],
            },
            new("isSet", IsSet)
            {
                Line = 0,
                ParameterVariables =
                [
                    new VariableDeclarationNode()
                    {
                        Name = "refersTo",
                        Type = new ReferenceType(
                            new GenericType(
                                "R",
                                new FunctionGenericContext(
                                    "isSet",
                                    BuiltInFunctionNode.BuiltinModuleName,
                                    new TypeIndex([])
                                )
                            )
                        ),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new BooleanType(),
                        IsConstant = false
                    }
                ],
                GenericTypeParameterNames = ["R"],
            },
            new("size", Size)
            {
                Line = 0,
                ParameterVariables =
                [
                    new VariableDeclarationNode()
                    {
                        Name = "refersTo",
                        Type = new ReferenceType(
                            new GenericType(
                                "R",
                                new FunctionGenericContext(
                                    "size",
                                    BuiltInFunctionNode.BuiltinModuleName,
                                    new TypeIndex([])
                                )
                            )
                        ),
                        IsConstant = true
                    },
                    new VariableDeclarationNode()
                    {
                        Name = "value",
                        Type = new IntegerType(),
                        IsConstant = false
                    }
                ],
                GenericTypeParameterNames = ["R"],
            },
        };
        foreach (var f in retVal)
        {
            functionList[f.Name] = f;
        }

        numberOfBuiltInFunctions = retVal.Count;
    }

    public static BuiltInFunctionNode MakeNode(
        //string namePrefix,
        string name,
        VariableDeclarationNode[] parameters,
        BuiltInFunctionNode.BuiltInCall call
    )
    {
        var retVal = new BuiltInFunctionNode(name, call);
        retVal.ParameterVariables.AddRange(parameters);
        return retVal;
    }

    private static readonly Random R = new();

    //change the paramters to expression node
    //line 881
    // public static void Times(List<InterpreterDataType> parameters)
    // {
    //     if (parameters[0] is IntDataType intParam && parameters[1] is IteratorDataType dest)
    //     {
    //         int count = intParam.Value;
    //         var iteratorDataType = new IteratorDataType(count);
    //
    //         dest.Value = iteratorDataType.Value;
    //     }
    //     else
    //     {
    //         throw new Exception("First parameter must be an integer parameter.");
    //     }
    // }

    public static void Clone(List<InterpreterDataType> parameters)
    {
        List<string> stringRepresentation = new List<string>();

        foreach (var param in parameters)
        {
            stringRepresentation.Add(param.ToString());
        }

        if (parameters[0] is StringDataType dest)
        {
            //fix this
        }
    }

    public static void Left(List<InterpreterDataType> parameters)
    {
        if (
            parameters[0] is StringDataType ss
            && parameters[1] is IntDataType len
            && parameters[2] is StringDataType dest
        )
            dest.Value = ss.Value.Substring(0, len.Value);
        else
            throw new Exception("Left data types not correct");
    }

    public static void Right(List<InterpreterDataType> parameters)
    {
        if (
            parameters[0] is StringDataType ss
            && parameters[1] is IntDataType len
            && parameters[2] is StringDataType dest
        )
            dest.Value = ss.Value.Substring(ss.Value.Length - len.Value, len.Value);
        else
            throw new Exception("Right data types not correct");
    }

    public static void Substring(List<InterpreterDataType> parameters)
    {
        if (
            parameters[0] is StringDataType ss
            && parameters[1] is IntDataType index
            && parameters[2] is IntDataType len
            && parameters[3] is StringDataType dest
        )
            dest.Value = ss.Value.Substring(index.Value, len.Value);
        else
            throw new Exception("Substring data types not correct");
    }

    public static void IntegerToReal(List<InterpreterDataType> parameters)
    {
        if (parameters[1] is FloatDataType f && parameters[0] is IntDataType i)
            f.Value = i.Value * 1.0f;
        else
            throw new Exception("IntegerToReal data types not correct");
    }

    public static void RealToInteger(List<InterpreterDataType> parameters)
    {
        if (parameters[0] is FloatDataType f && parameters[1] is IntDataType i)
            i.Value = (int)f.Value;
        else
            throw new Exception("RealToInteger data types not correct");
    }

    public static void Random(List<InterpreterDataType> parameters)
    {
        if (parameters[0] is IntDataType i)
            i.Value = R.Next();
        else
            throw new Exception("Random data types not correct");
    }

    public static void SquareRoot(List<InterpreterDataType> parameters)
    {
        if (parameters[0] is FloatDataType f && parameters[1] is FloatDataType r)
            r.Value = (float)Math.Sqrt((double)f.Value);
        else
            throw new Exception("SquareRoot data types not correct");
    }

    public static void Write(List<InterpreterDataType> parameters)
    {
        foreach (var p in parameters)
            Console.Write(p.ToString() + " ");
        Console.WriteLine();
    }

    public static void Write2(List<InterpreterDataType> parameters)
    {
        Console.Write(
            parameters.Append(new StringDataType("\n")).Select(idt => idt + " ").ToString()
        );
    }

    public static void WriteToTest(List<InterpreterDataType> parameters)
    {
        foreach (var p in parameters)
            Interpreter.testOutput.Append(p.ToString() + " ");
    }

    public static void Read(List<InterpreterDataType> parameters)
    {
        var line = Console.ReadLine();
        if (line == null)
            throw new Exception("Read failed - no data available.");
        var items = line.Split(" ");
        for (var i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            p.FromString(items[i]);
        }
    }

    public static void AssertIsEqual(List<InterpreterDataType> parameters)
    {
        Object j;
        Object i;
        if (parameters.ElementAt(0) is IntDataType)
        {
            if (parameters.ElementAt(1) is not IntDataType)
                throw new Exception(
                    $"assertIsEqual cannot compare the types {parameters.ElementAt(0).GetType()} and {parameters.ElementAt(1).GetType()}."
                );
            int.TryParse(parameters.ElementAt(0).ToString(), out int k);
            int.TryParse(parameters.ElementAt(1).ToString(), out int f);
            i = k;
            j = f;
        }
        else if (parameters.ElementAt(0) is FloatDataType)
        {
            if (parameters.ElementAt(1) is not FloatDataType)
                throw new Exception(
                    $"assertIsEqual cannot compare the types {parameters.ElementAt(0).GetType()} and {parameters.ElementAt(1).GetType()}."
                );
            float.TryParse(parameters.ElementAt(0).ToString(), out float k);
            float.TryParse(parameters.ElementAt(0).ToString(), out float f);
            i = k;
            j = f;
        }
        else if (parameters.ElementAt(0) is BooleanDataType)
        {
            if (parameters.ElementAt(1) is not BooleanDataType)
                throw new Exception(
                    $"assertIsEqual cannot compare the types {parameters.ElementAt(0).GetType()} and {parameters.ElementAt(1).GetType()}."
                );
            bool.TryParse(parameters.ElementAt(0).ToString(), out bool k);
            bool.TryParse(parameters.ElementAt(0).ToString(), out bool f);
            i = k;
            j = f;
        }
        else if (parameters.ElementAt(0) is CharDataType)
        {
            if (parameters.ElementAt(1) is not CharDataType)
                throw new Exception(
                    $"assertIsEqual cannot compare the types {parameters.ElementAt(0).GetType()} and {parameters.ElementAt(1).GetType()}."
                );
            i = parameters.ElementAt(0).ToString().ElementAt(0);
            j = parameters.ElementAt(1).ToString().ElementAt(0);
        }
        else
        {
            if (
                parameters.ElementAt(0) is not StringDataType
                || parameters.ElementAt(1) is not StringDataType
            )
                throw new Exception(
                    $"assertIsEqual cannot compare the types {parameters.ElementAt(0).GetType()} and {parameters.ElementAt(1).GetType()}."
                );
            i = parameters.ElementAt(0).ToString();
            j = parameters.ElementAt(1).ToString();
        }
        //else if (parameters.ElementAt(0) is ArrayDataType)
        //{

        //}

        Program.UnitTestResults.Last().Asserts.Last().passed = i.Equals(j);
        Program.UnitTestResults.Last().Asserts.Last().comparedValues =
            $"Expected<{i}>, Actual<{j}>";
    }

    private static void Low(List<InterpreterDataType> parameters)
    {
        if (parameters is [ArrayDataType adt, IntDataType idt])
        {
            idt.Value = (int)adt.Type.Range.From;
        }
        else
        {
            throw new Exception("Low requires the following parameters: array, var integer");
        }
    }

    private static void High(List<InterpreterDataType> parameters)
    {
        if (parameters is [ArrayDataType adt, IntDataType idt])
        {
            idt.Value = (int)adt.Type.Range.To;
        }
        else
        {
            throw new Exception("High requires the following parameters: array, var integer");
        }
    }

    public static void AllocateMemory(List<InterpreterDataType> parameters)
    {
        if (parameters.TryGetIdt(0, out var maybeRefDt) && maybeRefDt is ReferenceDataType refDt)
        {
            refDt.Record = (RecordDataType)refDt.RecordType.ToIdt();
            refDt.Record.Value = refDt
                .Record.getMemberTypes()
                .Select(
                    kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value.ToIdtDangerous(null))
                )
                .ToDictionary();
            return;
        }

        //if (
        //    parameters.TryGetIdt(1, out var failSilently)
        //    && failSilently is BooleanDataType { Value: true }
        //)
        //    return;

        throw new Exception("Can only allocate memory for record pointers.");
    }

    public static void FreeMemory(List<InterpreterDataType> parameters)
    {
        if (parameters[0] is ReferenceDataType rdt)
            rdt.Record = null;
        else
            throw new Exception("Can only free memory for record pointers.");
    }

    public static void IsSet(List<InterpreterDataType> parameters)
    {
        if (parameters[0] is ReferenceDataType rdt && parameters[1] is BooleanDataType bdt)
            bdt.Value = rdt.Record != null;
        else
            throw new Exception(
                "IsSet requires the following parameters: record pointer, var boolean"
            );
    }

    public static void Size(List<InterpreterDataType> parameters)
    {
        if (parameters[0] is ReferenceDataType rdt && parameters[1] is IntDataType idt)
        {
            if (rdt.Record != null)
                idt.Value = recursiveRecordSize(rdt.Record);
            else
                throw new Exception(
                    "Cannot call size on a record pointer that has not been allocated"
                );
        }
        else
            throw new Exception(
                "Size requires the following parameters: record pointer, var integer"
            );
    }

    private static int recursiveRecordSize(RecordDataType rdt)
    {
        int size = 0;
        foreach (var recordMember in rdt.getMemberTypes())
        {
            var rm = recordMember.Value;
            switch (rm)
            {
                case IntegerType:
                    size += sizeof(int);
                    break;
                case RealType:
                    size += sizeof(float);
                    break;
                case StringType:
                    size += ((string)rdt.Value[recordMember.Key]).Length * 2;
                    break;
                case CharacterType:
                    size += sizeof(char);
                    break;
                case BooleanType:
                    size += 1;
                    break;
                case RecordType:
                    //size += recursiveRecordSize(rmn.);
                    break;
                case ReferenceType:
                    //size += recursiveRecordSize(recordMember);
                    break;
            }
        }

        return size;
    }
}
