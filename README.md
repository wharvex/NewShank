### Last updated: 3/27

Recommended IDE: VSCode. As .csproj states, LLVMSharp.INterop Version=15.0.0-beta1 is used.

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
