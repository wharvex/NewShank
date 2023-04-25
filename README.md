### Last updated: 4/25

- Experssion.cs: Loop can be generated, but needs revision.

Recommended IDE: VSCode. As .csproj states, LLVMSharp.Interop Version=15.0.0-beta1 is used.

We should use integer i64 and not i32 as IPA runs on i64.

Origianl fibonacci.shank is not used yet. (For loop increment variable needs to be done and generality needs to be implemented)

To convert IR to RISC-V: llc -march=riscv32 <input_file>.ll -o <output_file>.s

