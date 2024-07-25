function b {
    "Running Python (Interpreted)"
    "Running Python (Interpreted)`n" > BenchmarkScriptOutput.txt

    Measure-Command { python .\Shank\benchMark\Fibonacci\Fibonacci.py `
    *>> BenchmarkScriptOutput.txt }

    "Running Shank (Interpreted) with No Vuop`n"
    "Running Shank (Interpreted) with No Vuop`n" >> BenchmarkScriptOutput.txt

    Measure-Command { dotnet run Interpret .\Shank\benchMark\Fibonacci `
    --project .\Shank\Shank.csproj *>> BenchmarkScriptOutput.txt }

    "Running Shank (Interpreted) with Vuop`n"
    "Running Shank (Interpreted) with Vuop`n" >> BenchmarkScriptOutput.txt

    Measure-Command { dotnet run Interpret .\Shank\benchMark\Fibonacci `
    --vuop-test --project .\Shank\Shank.csproj *>> BenchmarkScriptOutput.txt }

    "Compiling Shank with No Vuop`n" >> BenchmarkScriptOutput.txt

    dotnet run Compile .\Shank\benchMark\Fibonacci --print-ir --linker clang `
    -o .\TestCompileOutput\fibShankNoVuop.exe --project .\Shank\Shank.csproj `
    *>> BenchmarkScriptOutput.txt

    "Compiling Shank with Vuop`n" >> BenchmarkScriptOutput.txt

    dotnet run Compile .\Shank\benchMark\Fibonacci --print-ir --vuop-test `
    --linker clang -o .\TestCompileOutput\fibShankVuop.exe `
    --project .\Shank\Shank.csproj *>> BenchmarkScriptOutput.txt

    "Compiling C`n" >> BenchmarkScriptOutput.txt

    clang .\Shank\benchMark\Fibonacci\Fibonacci.c `
    -o .\TestCompileOutput\fibC.exe *>> BenchmarkScriptOutput.txt

    "Running C (Compiled Executable)`n" >> BenchmarkScriptOutput.txt
    "Running C (Compiled Executable)`n"

    Measure-Command { .\TestCompileOutput\fibC.exe `
    *>> BenchmarkScriptOutput.txt }

    "Running Shank (Compiled Executable) with No Vuop`n" `
    >> BenchmarkScriptOutput.txt
    "Running Shank (Compiled Executable) with No Vuop`n"

    Measure-Command { .\TestCompileOutput\fibShankNoVuop.exe `
    *>> BenchmarkScriptOutput.txt }

    "Running Shank (Compiled Executable) with Vuop`n" `
    >> BenchmarkScriptOutput.txt
    "Running Shank (Compiled Executable) with Vuop`n"

    Measure-Command { .\TestCompileOutput\fibShankVuop.exe `
    *>> BenchmarkScriptOutput.txt }
}
