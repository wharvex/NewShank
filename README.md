### Last updated: 5/8

To generate RISC-V: llc -march=riscv64 output_ir_3.ll -o out3.s

fibonacci.shank & Math_Op.shank can be converted to RISC-V.

harmonic.shank can be converted to IR but not to RISC-V. Few more changes would require.

recursive for loop can be done as code has been divided into functions.
