function b {
    "Python`n"
    "Python`n" > BenchmarkScriptOutput.txt

    Measure-Command { python .\Shank\benchMark\Fibonacci\Fibonacci.py `
    *>> BenchmarkScriptOutput.txt }

    "Shank Interpret No Vuop`n"
    "Shank Interpret No Vuop`n" > BenchmarkScriptOutput.txt

    Measure-Command { dotnet run Interpret .\Shank\benchMark\Fibonacci `
    --project .\Shank\Shank.csproj >> BenchmarkScriptOutput.txt }

    "Shank Interpret Vuop`n"
    "Shank Interpret Vuop`n" >> BenchmarkScriptOutput.txt

    Measure-Command { dotnet run Interpret .\Shank\benchMark\Fibonacci `
    --vuop-test --project .\Shank\Shank.csproj >> BenchmarkScriptOutput.txt }

    "Shank Compile No Vuop`n" >> BenchmarkScriptOutput.txt

    dotnet run Compile .\Shank\benchMark\Fibonacci --print-ir --linker clang `
    -o .\TestCompileOutput\fibShankNoVuop.exe --project .\Shank\Shank.csproj `
    *>> BenchmarkScriptOutput.txt

    "Shank Compile Vuop`n" >> BenchmarkScriptOutput.txt

    dotnet run Compile .\Shank\benchMark\Fibonacci --print-ir --vuop-test `
    --linker clang -o .\TestCompileOutput\fibShankNoVuop.exe `
    --project .\Shank\Shank.csproj *>> BenchmarkScriptOutput.txt

    "C Compile`n" >> BenchmarkScriptOutput.txt

    clang .\Shank\benchMark\Fibonacci\Fibonacci.c `
    -o .\TestCompileOutput\fibC.exe *>> BenchmarkScriptOutput.txt

    "C Compiled Executable`n" >> BenchmarkScriptOutput.txt
    "C Compiled Executable`n"

    Measure-Command { .\TestCompileOutput\fibC.exe `
    *>> BenchmarkScriptOutput.txt }

    "Shank Compiled Executable`n" >> BenchmarkScriptOutput.txt
    "Shank Compiled Executable`n"

    Measure-Command { .\TestCompileOutput\fibShankNoVuop.exe `
    *>> BenchmarkScriptOutput.txt }
}
