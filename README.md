# The Shank Programming Langauge V3

## Required dependencies

- clang or a linker
- .NET runtime 8.0

## Setup

please check Shank.csproj to make sure you have the required dependencies downloaded. 

These commands will clone and run the project:
```SH
# clone project
git clone https://github.com/mphipps1/ShankCompiler.git
cd ShankCompile/Shank

# run different commands of shank

dotnet run -- Compile ./ShankTestFiles/HelloWorld.shank --print-ir -o HelloWorld.exe # runs compiler

dotnet run -- Interpret ./ShankTestFiles/HelloWorld.shank # runs interpreter

# powershell script testing

. .\ShankTestScripts.ps1
st 
```
if done correctly you should see a "hello world" in the console
depending on what you run

for a more detailed explanation about each CLI command
<a href=https://github.com/mphipps1/ShankCompiler/wiki/Shank-CLI-(Command-Line-Interface)-documentation>CLI documentation</a>


## Formatting

This project has a GitHub Action that uses the CSharpier formatter to format all .cs files in the repo.

The action is triggered when pushing to the master branch. It formats if needed and creates a new commit if formatting occurs.

If formatting occurs and the commit is pushed, please remember to pull the changes to your local repo afterwards.

# Documentation

- <a href=https://github.com/mphipps1/ShankCompiler/wiki>Documentation</a>