using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShankUnitTests
{
    [TestClass]
    public class EnumUnitTests
    {
        public ModuleNode getModuleFromParser(string[] file)
        {
            return ModuleParserTests.getModuleFromParser(file);
        }

        [TestMethod]
        public void simpleEnumParse()
        {
            string[] file =
            {
                "enum colors = [red]\n",
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
            Assert.AreEqual(1, module.getEnums().Count());
            Assert.IsTrue(module.getEnums().ContainsKey("colors"));
            Assert.AreEqual(1, module.getEnums()["colors"].EnumElements.Count);
        }

        [TestMethod]
        public void simpleEnumParseMultipleElements()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
            Assert.AreEqual(1, module.getEnums().Count());
            Assert.IsTrue(module.getEnums().ContainsKey("colors"));
            Assert.AreEqual(3, module.getEnums()["colors"].EnumElements.Count);
        }

        [TestMethod]
        public void parseEnumAfterFunction()
        {
            string[] file =
            {
                "define start()\n",
                "variables i : integer\n",
                "\ti := 3\n",
                "enum colors = [red, green, blue]\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
            Assert.AreEqual(1, module.getEnums().Count());
            Assert.IsTrue(module.getEnums().ContainsKey("colors"));
            Assert.AreEqual(3, module.getEnums()["colors"].EnumElements.Count);
        }

        //checking if the line "e := red" would be parsed without throwing an error
        [TestMethod]
        public void parseEnumVariable()
        {
            string[] file =
            {
                "enum colors = [red, green, blue]\n",
                "define start()\n",
                "variables i : integer\n",
                "variables e : colors\n",
                "\ti := 3\n",
                "\te := red\n"
            };
            ModuleNode module = getModuleFromParser(file);
            Assert.IsNotNull(module);
        }
    }
}
