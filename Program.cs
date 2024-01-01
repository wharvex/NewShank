﻿using System.Collections;

namespace Shank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var inPaths = new List<string>();
            if (args.Length < 1)
            {
                Directory
                    .GetFiles(
                        Directory.GetCurrentDirectory(),
                        "*.shank",
                        SearchOption.AllDirectories
                    )
                    .ToList()
                    .ForEach(inPaths.Add);
            }
            else
            {
                if (Directory.Exists(args[0]))
                {
                    Directory
                        .GetFiles(args[0], "*.shank", SearchOption.AllDirectories)
                        .ToList()
                        .ForEach(inPaths.Add);
                }
                else if (File.Exists(args[0]) && args[0].EndsWith(".shank"))
                {
                    inPaths.Add(args[0]);
                }
            }

            if (inPaths.Count < 1)
            {
                throw new Exception(
                    "Options when calling shank from Command Line (CL): "
                        + "1) pass a valid path to a *.shank file; "
                        + "2) pass a valid path to a directory containing at least one *.shank file; "
                        + "3) have at least one *.shank file in the current directory and pass no CL arguments"
                );
            }

            foreach (var inPath in inPaths)
            {
                var lines = File.ReadAllLines(inPath);
                var tokens = new List<Token>();
                var l = new Lexer();
                tokens.AddRange(l.Lex(lines));

                // Prepare to prepend any function name with the name of the file it is in.
                // Technically, a function's name will be prepended with the path of the
                // current file relative to the path of the current directory.
                // This is to facilitate multi-file parsing/interpreting.
                var newFbNameBase =
                    Path.GetRelativePath(Directory.GetCurrentDirectory(), inPath)[
                        ..^(".shank".Length)
                    ]
                        .Replace('.', '_')
                        .Replace('\\', '_')
                        .Replace('/', '_') + '_';
                var p = new Parser(tokens, newFbNameBase);

                var brokeOutOfWhile = false;
                while (tokens.Any())
                {
                    FunctionNode? fb = null;
                    var errorOccurred = false;
                    try
                    {
                        fb = p.Function();
                    }
                    catch (SyntaxErrorException e)
                    {
                        Console.WriteLine(
                            $"\nException encountered in file {inPath}:\n{e.Message}\nskipping..."
                        );
                        errorOccurred = true;
                    }

                    if (errorOccurred)
                    {
                        brokeOutOfWhile = true;
                        break;
                    }

                    if (fb == null)
                    {
                        continue;
                    }
                    Interpreter.Functions.Add(fb.Name, fb);

                    // fb.LLVMCompile();
                }

                if (brokeOutOfWhile)
                {
                    continue;
                }

                Console.WriteLine($"\nOutput of {inPath}:\n");
                BuiltInFunctions.Register(Interpreter.Functions, newFbNameBase);
                if (
                    Interpreter.Functions.ContainsKey(newFbNameBase + "start")
                    && Interpreter.Functions[newFbNameBase + "start"] is FunctionNode s
                )
                {
                    Interpreter.InterpretFunction(s, new List<InterpreterDataType>());
                }
            }
        }
    }
}
