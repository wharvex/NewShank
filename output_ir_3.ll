; ModuleID = 'main'
source_filename = "main"

declare void @write(i64* %value)

define void @start() {
entry:
  %start = alloca i64, align 4
  store i64 1, i64* %start, align 4
  %end = alloca i64, align 4
  store i64 20, i64* %end, align 4
  %i = alloca i64, align 4
  %prev1 = alloca i64, align 4
  %prev2 = alloca i64, align 4
  %curr = alloca i64, align 4
  store i64 1, i64* %prev1, align 4
  store i64 1, i64* %prev2, align 4
  call void @write(i64* %prev1)
  call void @write(i64* %prev2)
  br label %loop

loop:                                             ; preds = %loop, %entry
  %counter = alloca i64, align 4
  store i64 0, i64* %counter, align 4
  call void @write(i64* %prev1)

ret void
}