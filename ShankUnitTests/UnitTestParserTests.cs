using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ShankUnitTests
{
    [TestClass]
    public class UnitTestParserTests
    {
        public ModuleNode getModuleFromLexer(string[] file)
        {
            return ModuleParserTests.getModuleFromLexer(file);
        }

        [TestMethod]
        public void simpleUnitTest()
        {
            string[] file1 =
            {
                "define start()\n",
                "variables p : integer\n",
                "\tp := 3\n",
                "\n",
                "define add(a, b : integer; var c : integer)\n",
                "\tc := a + b\n",
                "\n",
                "test simpleTest for add(a, b : integer; var c : integer)\n",
                "variables j : integer\n",
                "\tadd 1, 2, var j\n",
                "\tassertIsEqual 3, j\n"
            };
            ModuleNode m = getModuleFromLexer(file1);
            Assert.AreEqual(m.getTests().Count, 1);
            Assert.IsTrue(m.getTests().ContainsKey("simpleTest"));
            Assert.IsTrue(m.getTests()["simpleTest"].targetFunctionName == "add");
            Assert.IsTrue(m.getTests()["simpleTest"].ParameterVariables.Count == 3);
        }
    }
}
