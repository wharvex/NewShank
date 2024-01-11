; ModuleID = 'root'
source_filename = "root"

@0 = private unnamed_addr constant [15 x i8] c"Hello, World!\0A\00", align 1

define i32 @main(i32 %0) {
entry:
  call void (ptr, ...) @printf(ptr @0)
  ret i32 0
}

declare void @printf(ptr, ...)
