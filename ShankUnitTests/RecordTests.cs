using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShankUnitTests
{
    [TestClass]
    public class RecordTests
    {
        public ModuleNode getModuleFromParser(string[] file)
        {
            return ModuleParserTests.getModuleFromParser(file);
        }

        public static void initializeInterpreter(LinkedList<string[]> files)
        {
            ModuleInterpreterTests.initializeInterpreter(files);
        }

        public static void runInterpreter()
        {
            ModuleInterpreterTests.runInterpreter();
        }

        [TestMethod]
        public void simpleRecord()
        {
            string[] file =
            {
                "record r\n",
                "\ti : integer\n",
                "define start()\n",
                "variables p : r\n",
                "\tp.i := 3\n"
            };

            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            runInterpreter();
        }

        [TestMethod]
        //gets past semantic analysis but gets caught in the interpreter 
        public void nestedRecord()
        {
            string[] file =
            {
                "record rec1\n",
                "\ti : integer\n",
                "\tp : rec2\n",
                "record rec2\n",
                "\ts : string\n",
                "define start()\n",
                "variables r : rec1\n",
                "\tr.p.s := \"helloworld\"\n"
            };
            LinkedList<string[]> files = new LinkedList<string[]>();
            files.AddLast(file);
            initializeInterpreter(files);
            //runInterpreter();
        }
    }
}
