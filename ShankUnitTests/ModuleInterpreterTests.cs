using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShankUnitTests
{
    [TestClass]
    public class ModuleInterpreterTests
    {
        private int unnamedModuleCount = 0;
        private StringBuilder ConsoleOutput { get; set; }
        public Dictionary<string, ModuleNode> getModulesFromParser(LinkedList<string[]> list)
        {
            Dictionary<string, ModuleNode> Modules = new Dictionary<string, ModuleNode>();
            Lexer l = new Lexer();
            foreach (string[] file in list)
            {
                Parser p = new Parser(l.Lex(file));
                ModuleNode m = p.Module();
                if (m.getName() == null)
                {
                    m.setName(unnamedModuleCount.ToString());
                    unnamedModuleCount++;
                } 
                Modules.Add(m.getName(), m);
            }
            return Modules;
        }
        public void initializeInterpreter(LinkedList<string[]> files)
        {
            Interpreter.reset();
            Dictionary<string, ModuleNode> modules = getModulesFromParser(files);
            Interpreter.setModules(modules);
            Interpreter.setStartModule();
            Interpreter.handleExports();
            Interpreter.handleImports();
        }
        public void runInterpreter()
        {
            foreach (KeyValuePair<string, ModuleNode> currentModulePair in Interpreter.Modules)
            {
                var currentModule = currentModulePair.Value;
                //Console.WriteLine($"\nOutput of {currentModule.getName()}:\n");

                BuiltInFunctions.Register(currentModule.getFunctions());
                if (
                    currentModule.getFunctions().ContainsKey("start")
                    && currentModule.getFunctions()["start"] is FunctionNode s
                )
                {
                    var interpreterErrorOccurred = false;
                    try
                    {
                        Interpreter.InterpretFunction(s, new List<InterpreterDataType>());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(
                            $"\nInterpretation error encountered in file {currentModulePair.Key}:\n{e}\nskipping..."
                        );
                        interpreterErrorOccurred = true;
                    }

                    if (interpreterErrorOccurred)
                    {
                        continue;
                    }
                }

                // if (count < 1)
                // {
                //     var gen = new IRGenerator(newFnNamePrefix);
                //     gen.GenerateIR();
                // }

                // count++;
            }
        }
        [TestMethod]
        public void simpleInterpreterTest()
        {
            string[] file1 = {
                "define start()\n",
                "variables p : integer\n",
                    "\tp:=3\n",
                    "\twriteToTest p\n"
                 };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file1);
            initializeInterpreter(files);
            runInterpreter();
            int.TryParse(Interpreter.testOutput[0].ToString(), out int j);
            Assert.AreEqual(j, 3);
        }

        [TestMethod]
        public void simpleImportAndExport()
        {
            string[] file1 = {"module test1\n",
                              "import test2\n",
                              "define start()\n",
                              "variables p : integer\n",
                                 "\tadd 1,2, var p\n",
                                 "\twrite p\n"};
            string[] file2 = { "module test2\n",
                              "export add\n",
                              "define add(a, b : integer; var c : integer)\n",
                                 "\tc := a + b\n" };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddFirst(file1);
            files.AddLast(file2);
            initializeInterpreter(files);
            runInterpreter();
        }
    }
}
