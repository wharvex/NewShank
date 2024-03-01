; ModuleID = 'main'
source_filename = "main"

declare i64 @write(i64)

define i64 @start() {
entry:
  %start = alloca i64, align 4
  store i64 1, i64* %start, align 4
  %end = alloca i64, align 4
  store i64 20, i64* %end, align 4
  %i = alloca i64, align 4
  %prev1 = alloca i64, align 4
  %prev2 = alloca i64, align 4
  %curr = alloca i64, align 4
  %0 = load i64, i64* %start, align 4
  store i64 %0, i64* %prev1, align 4
  %1 = load i64, i64* %start, align 4
  store i64 %1, i64* %prev2, align 4
  %2 = load i64, i64* %prev1, align 4
  %write = call i64 @write(i64 %2)
  %3 = load i64, i64* %prev2, align 4
  %write1 = call i64 @write(i64 %3)
  %4 = load i64, i64* %start, align 4
  store i64 %4, i64* %i, align 4
  br label %for.condition

for.condition:                                    ; preds = %for.body, %entry
  %5 = load i64, i64* %i, align 4
  %6 = load i64, i64* %end, align 4
  %loop.condition.cmp = icmp slt i64 %5, %6
  br i1 %loop.condition.cmp, label %for.body, label %exit

for.body:                                         ; preds = %for.condition
  %7 = load i64, i64* %prev1, align 4
  %8 = load i64, i64* %prev2, align 4
  %plus = add i64 %7, %8
  store i64 %plus, i64* %curr, align 4
  %9 = load i64, i64* %curr, align 4
  %write2 = call i64 @write(i64 %9)
  %10 = load i64, i64* %prev1, align 4
  store i64 %10, i64* %prev2, align 4
  %11 = load i64, i64* %curr, align 4
  store i64 %11, i64* %prev1, align 4
  %12 = load i64, i64* %i, align 4
  %i3 = add i64 %12, 1
  store i64 %i3, i64* %i, align 4
  br label %for.condition

exit:                                             ; preds = %for.condition
  ret i64 0
}
