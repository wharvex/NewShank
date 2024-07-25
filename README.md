# Shank Programming langauge
## How to run the compiler

These commands will clone and run the project:
```
git clone https://github.com/mphipps1/ShankCompiler.git
cd shank
# different commands

dotnet run -- Compile ./ShankTestFiles/Fibonacci.shank --print-ir -o Fibonacci.exe # runs compiler
dotnet run -- Interpret ./ShankTestFiles/Fibonacci.shank # runs interpreter
. .\ShankTestScripts.ps1
st # powershell script testing refer to dotshank dir for explanation 
```
refer to the wiki if you want a detailed explanation of the commands.

If you pass in the absolute/relative path of a shank file it just interprets that file.

If you pass in the path of a directory it recursively interprets all *.shank files in that directory.

You can list different files and it compiles/interprets each one. please do mind they will
be treated like each of the files is 1 program.


## Formatting

This project has a GitHub Action that uses the CSharpier formatter to format all .cs files in the repo.

The action is triggered when pushing to the master branch. It formats if needed and creates a new commit if formatting occurs.

If formatting occurs and the commit is pushed, please remember to pull the changes to your local repo afterwards.
