namespace Shank
{
    public class BuiltInFunctions
    {
        public static int numberOfBuiltInFunctions = 0;

        public static void Register(
            Dictionary<string, CallableNode> functionList
        //string functionNameBase
        )
        { // Note to the reader - this implementation is different than what I have the students writing in Java.
            // The concepts are the same, but this is more language appropriate. This would be too hard for
            // the students to do in Java.
            var retVal = new List<BuiltInFunctionNode>
            {
                MakeNode("write", new VariableNode[] { }, Write, true),
                MakeNode("read", new VariableNode[] { }, Read, true),
                MakeNode("writeToTest", new VariableNode[] { }, WriteToTest, true),
                MakeNode(
                    "squareRoot",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.Real,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.Real,
                            IsConstant = false
                        },
                    },
                    SquareRoot,
                    false
                ),
                MakeNode(
                    "getRandom",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = false
                        },
                    },
                    Random,
                    false
                ),
                MakeNode(
                    "integerToReal",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.Real,
                            IsConstant = false
                        },
                    },
                    IntegerToReal,
                    false
                ),
                MakeNode(
                    "realToInteger",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.Real,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = false
                        },
                    },
                    RealToInteger,
                    false
                ),
                MakeNode(
                    "left",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.String,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "amount",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.String,
                            IsConstant = false
                        },
                    },
                    Left,
                    false
                ),
                MakeNode(
                    "right",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.String,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "amount",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.String,
                            IsConstant = false
                        },
                    },
                    Right,
                    false
                ),
                MakeNode(
                    "substring",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.String,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "index",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "amount",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "result",
                            Type = VariableNode.DataType.String,
                            IsConstant = false
                        },
                    },
                    Substring,
                    false
                ),
                MakeNode(
                    "assertIsEqual",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "actualValue",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "targetValue",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = true
                        }
                    },
                    AssertIsEqual,
                    false
                ),
                MakeNode(
                    "allocateMemory",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "refersTo",
                            Type = VariableNode.DataType.Reference,
                            IsConstant = false
                        }
                    },
                    AllocateMemory,
                    false
                    ),
                MakeNode(
                    "freeMemory",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "refersTo",
                            Type = VariableNode.DataType.Reference,
                            IsConstant = false
                        }
                    },
                    FreeMemory,
                    false
                    ),
                MakeNode(
                    "isSet",
                     new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "refersTo",
                            Type = VariableNode.DataType.Reference,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.Boolean,
                            IsConstant = false
                        }
                    },
                     IsSet,
                     false
                    ),
                MakeNode(
                    "size",
                    new VariableNode[]
                    {
                        new VariableNode()
                        {
                            Name = "refersTo",
                            Type = VariableNode.DataType.Reference,
                            IsConstant = true
                        },
                        new VariableNode()
                        {
                            Name = "value",
                            Type = VariableNode.DataType.Integer,
                            IsConstant = false
                        }
                    },
                    Size,
                    false
                    )
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
            VariableNode[] parameters,
            BuiltInFunctionNode.BuiltInCall call,
            bool isVariadic = false
        )
        {
            var retVal = new BuiltInFunctionNode(name, call);
            retVal.ParameterVariables.AddRange(parameters);
            retVal.IsVariadic = isVariadic;
            return retVal;
        }

        private static readonly Random R = new();

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

            Program.unitTestResults.Last().Asserts.Last().passed = i.Equals(j);
            Program.unitTestResults.Last().Asserts.Last().comparedValues =
                $"Expected<{i}>, Actual<{j}>";
        }

        public static void AllocateMemory(List<InterpreterDataType> parameters)
        {
            if (parameters[0] is ReferenceDataType rdt)
                rdt.Record = new RecordDataType(rdt.RecordType.Members);
            else
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
            if (parameters[0] is ReferenceDataType rdt
                && parameters[1] is BooleanDataType bdt)
                bdt.Value = rdt.Record != null;
            else
                throw new Exception("IsSet requires the following parameters: record pointer, var boolean");
        }

        public static void Size(List<InterpreterDataType> parameters)
        {
            if (parameters[0] is ReferenceDataType rdt
                && parameters[1] is IntDataType idt)
            {
                if (rdt.Record != null)
                    idt.Value = recursiveRecordSize(rdt.Record);
                else
                    throw new Exception("Cannot call size on a record pointer that has not been allocated");
            }

            else
                throw new Exception("Size requires the following parameters: record pointer, var integer");
        }

        private static int recursiveRecordSize(RecordDataType rdt)
        {
            int size = 0;
            foreach(var recordMember in rdt.MemberTypes)
            {
                var rm = recordMember.Value;
                switch (rm)
                {
                    case VariableNode.DataType.Integer:
                        size+=sizeof(int);
                        break;
                    case VariableNode.DataType.Real:
                        size += sizeof(float);
                        break;
                    case VariableNode.DataType.String:
                        size += ((string)rdt.Value[recordMember.Key]).Length * 2;
                        break;
                    case VariableNode.DataType.Character:
                        size += sizeof(char);
                        break;
                    case VariableNode.DataType.Boolean:
                        size += 1;
                        break;
                    case VariableNode.DataType.Record:
                        //size += recursiveRecordSize(rmn.);
                        break;
                    case VariableNode.DataType.Reference:
                        //size += recursiveRecordSize(recordMember);
                        break;
                        
                }
            }
            return size;
        }
    }
}
