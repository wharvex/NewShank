### Last updated: 3/27

In order to run (I have used VSCode), one one change is required in VSCode .csproj file. Add the following line:
 - <ItemGroup>
 -   <PackageReference Include="LLVMSharp.Interop" Version="15.0.0-beta1" />
 -   <PackageReference Include="libLLVM.runtime.win-x64" Version="15.0.0" />
 - </ItemGroup>

We should use integer i64 and not i32 as IPA runs on i64.

Origianl fibonacci.shank is not used yet. Rather, the below has been used: 

- define start()
- constants start = 1, end = 20
- variables i,prev1 : integer
- variables prev2,curr : integer
-	 prev1:=start
-	 prev2:=start
-	 write prev1
-	 write prev2

When running the above fibonacci.shank with the current version of code, the resulting IR is shown in fibonacci_output.txt
