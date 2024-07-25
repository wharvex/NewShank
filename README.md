# Shank Programming langauge
## How to run the compiler

These commands will clone and run the project:
```SH
git clone https://github.com/mphipps1/ShankCompiler.git
cd ShankCompile/Shank

# run different commands of shank

dotnet run -- Compile ./ShankTestFiles/Fibonacci.shank --print-ir -o Fibonacci.exe # runs compiler
dotnet run -- Interpret ./ShankTestFiles/Fibonacci.shank # runs interpreter
. .\ShankTestScripts.ps1
st # powershell script testing
```

If you pass in the absolute/relative path of a shank file it just interprets that file.

If you pass in the path of a directory it recursively interprets all *.shank files in that directory.

You can list different files and it compiles/interprets each one. please do mind they will
be treated like each of the files is 1 program.

please read the wiki on github if you are confused about how the code works
there is a PDF of the shank langauge definition in the "PDF's" directory please refer to that
to learn the syntax. 

## Formatting

This project has a GitHub Action that uses the CSharpier formatter to format all .cs files in the repo.

The action is triggered when pushing to the master branch. It formats if needed and creates a new commit if formatting occurs.

If formatting occurs and the commit is pushed, please remember to pull the changes to your local repo afterwards.
