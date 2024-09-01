# Shank Programming Langauge V3

## Required dependencies

these are things you need to be able to run this program as a whole

-  the newest version of the Clang and the LLVM
- Newest version of .NET run time 

### verify
<details>
 <summary>check if you have everything installed and set up correctly</summary>

```sh
clang -v
dotnet --version
```
</details>

## Build Instructions

⚠ Read up to make sure you have all the required dependencies downloaded 

and saved to your bin directory/PATH  ⚠

### clone and run the project
```SH
# clone project
git clone https://github.com/mphipps1/ShankCompiler.git
cd ShankCompiler/Shank

# runs compiler
dotnet run -- Compile ./ShankTestFiles/HelloWorld.shank --print-llvm -o HelloWorld.exe 

# runs Interpreter
dotnet run -- Interpret ./ShankTestFiles/HelloWorld.shank

# powershell script testing
cd ../
. .\ShankTestScripts.ps1
st 
```
if done correctly you should see a "hello world" in the console
depending on what you run

for a more detailed explanation about each CLI command
<a href=https://github.com/mphipps1/ShankCompiler/wiki/Shank-CLI-(Command-Line-Interface)-documentation>CLI documentation</a>


# Documentation

- <a href=https://github.com/mphipps1/ShankCompiler/wiki>Code Documentation</a>
- <a href=https://github.com/mphipps1/ShankCompiler/wiki/Shank-Language-Definition,-V3> Langauge Definition </a>
