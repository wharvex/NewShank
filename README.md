# ShankCompiler
## How to run
These commands will clone and run the project:
```
git clone https://github.com/mphipps1/ShankCompiler.git
cd ShankCompiler
dotnet run
```
If you pass in the absolute/relative path of a shank file it just interprets that file.

If you pass in the path of a directory it recursively interprets all *.shank files in that directory.

If you pass in no arguments it recursively interprets all *.shank files in the current directory.

It currently ignores all command line arguments except the first one. Stay tuned for variadic arguments in the future.

When parsing each function with Parser.function(), it catches any SyntaxErrorException, prints the exception message and skips to the next file.

## Formatting

This project has a GitHub Action that uses the CSharpier formatter to format all .cs files in the repo.

The action is triggered when pushing to the master branch. It formats if needed and creates a new commit if formatting occurs.

If formatting occurs and the commit is pushed, please remember to pull the changes to your local repo afterwards.
