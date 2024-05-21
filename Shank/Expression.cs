using System.Dynamic;
using System.Runtime.CompilerServices;
using LLVMSharp;

//To compile to RISC-V: llc -march=riscv64 output_ir_3.ll -o out3.s

namespace Shank;

public enum CrossFileInteraction
{
    Module,
    Export,
    Import
}
