using BenchmarkDotNet.Attributes;
using Shank;
using Shank.ASTNodes;
using Shank.WalkCompliantVisitors;

namespace BenchMark;

public class Setup
{
    public static ProgramNode pn;

    public static void ParseAndLex()
    {
        pn = new ProgramNode();
        Lexer l = new Lexer();
        string[] fibnauci = new string[]
        {
            "define start()",
            "constants start = 1, end = 100000",
            "variables i, prev1 : integer",
            "variables prev2, curr : integer",
            "    prev1 := start",
            "    prev2 := start",
            "    for i from start to end",
            "        curr := prev1 + prev2",
            "        prev2 := prev1",
            "        prev1 := curr"
        };

        string[] factroial = new string[]
        {
            "define start()",
            "variables n, n1, i, i1  : integer",
            "    n := 1",
            "    n1 := 4",
            "    for i1 from 1 to 1000000",
            "        for i from 1 to n1",
            "            n := n * i",
            "        n := 1",
            "    write \"hello world\""
        };

        string[] Manderbolt = new string[]
        {
            "define start()",
            "variables px, py, idx1, idx2, idx3 : integer",
            "variables x0, y0, x, y, xtemp, tmp: real",
            "    x0 := -1.0",
            "    y0 := 0.47",
            "    px := 0",
            "    py := 0",
            "    for px from 0 to 100",
            "        for py from 0 to 100",
            "            x := 0.0",
            "            y := 0.0",
            "            tmp := x * x + y * y",
            "            while tmp <= 4.0",
            "                idx3 := 0",
            "                while idx3 <= 10",
            "                    xtemp := x*x - y*y + x0",
            "                    y := 2.0 *x*y+y0",
            "                    x := xtemp",
            "                    idx3 := idx3 + 1",
            "                    tmp := x * x + y * y"
        };

        var Tokens = l.Lex(fibnauci);
        Parser p = new Parser(Tokens);
        var module = p.Module();
        pn.AddToModules(module);
        pn.SetStartModule();
        // SemanticAnalysis.ActiveInterpretOptions = options;
        BuiltInFunctions.Register(pn.GetStartModuleSafe().Functions);

        // SAVisitor.ActiveInterpretOptions = options;
        pn.Walk(new ImportVisitor());
        SemanticAnalysis.AreExportsDone = true;
        SemanticAnalysis.AreImportsDone = true;

        // This resolves unknown types.
        //program.Walk(new RecordVisitor());

        pn.Walk(new UnknownTypesVisitor());
        SemanticAnalysis.AreSimpleUnknownTypesDone = true;

        // program.Walk(new TestVisitor());

        // Create WalkCompliantVisitors.
        var nuVis = new NestedUnknownTypesResolvingVisitor(SemanticAnalysis.ResolveType);
        var vgVis = new VariablesGettingVisitor();
        var etVis = new ExpressionTypingVisitor(SemanticAnalysis.GetTypeOfExpression)
        {
            // ActiveInterpretOptions = options
        };

        // Apply WalkCompliantVisitors.
        pn.Walk(nuVis);
        SemanticAnalysis.AreNestedUnknownTypesDone = true;
        pn.Walk(vgVis);
        pn.Walk(etVis);

        //Run old SA.
        OutputHelper.DebugPrintJson(pn, "pre_old_sa");
        SemanticAnalysis.CheckModules(pn);
        OutputHelper.DebugPrintJson(pn, "post_old_sa");

        NewSemanticAnalysis.Run(pn);

        // RunInterpeter();
    }

    public static int RunInterpeter()
    {
        Interpreter.StartModule = pn.GetStartModuleSafe();
        Interpreter.Modules = pn.Modules;

        Interpreter.InterpretFunction(
            pn.GetStartModuleSafe().GetStartFunctionSafe(),
            [],
            pn.GetStartModuleSafe()
        );
        return 1;
    }
}

public class BenchMarkShank
{
    public BenchMarkShank()
    {
        Setup.ParseAndLex();
    }

    [Benchmark]
    public int RunInterper() => Setup.RunInterpeter();

    [Benchmark]
    public int RunInterper2() => 0;
}
