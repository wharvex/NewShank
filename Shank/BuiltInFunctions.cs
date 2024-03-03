namespace Shank
{
    public class BuiltInFunctions
    {
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
            };
            foreach (var f in retVal)
            {
                functionList[f.Name] = f;
            }
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
    }
}
