### Last updated: 4/9

Recommended IDE: VSCode. As .csproj states, LLVMSharp.Interop Version=15.0.0-beta1 is used.

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

fibonacci_output.txt: When running the above fibonacci.shank with the current version of code, the resulting IR is shown in this file.

riscv.txt: When converting the generated IR to RISC-V assembly through chat-gpt, the result is shown in this file.

