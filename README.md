### Last updated: 3/27

In order to run (I have used VSCode), one must have the following line in .csproj file: <PackageReference Include="LLVMSharp.Interop" Version="15.0.0-beta1" />

Origianl fibonacci.shank is not used. Rather, the below has been used: 

- define start()
- constants start = 1, end = 20
- variables i,prev1 : integer
- variables prev2,curr : integer
-	 prev1:=start
-	 prev2:=start
-	 write prev1
-	 write prev2
