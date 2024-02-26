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
  %index = alloca i64, align 4
  %val = alloca i64, align 4
  %curr = alloca i64, align 4
  %0 = load i64, i64* %start, align 4
  store i64 %0, i64* %val, align 4
  %1 = load i64, i64* %start, align 4
  %2 = load i64, i64* %end, align 4
  %divide = sdiv i64 %2, 10
  %plus = add i64 %1, %divide
  %times = mul i64 3, %plus
  store i64 %times, i64* %val, align 4
  %3 = load i64, i64* %val, align 4
  %write = call i64 @write(i64 %3)
  %4 = load i64, i64* %start, align 4
  store i64 %4, i64* %i, align 4
  br label %for.condition

for.condition:                                    ; preds = %for.body, %entry
  %5 = load i64, i64* %i, align 4
  %6 = load i64, i64* %end, align 4
  %loop.condition.cmp = icmp slt i64 %5, %6
  br i1 %loop.condition.cmp, label %for.body, label %exit

for.body:                                         ; preds = %for.condition
  %7 = load i64, i64* %val, align 4
  %8 = load i64, i64* %i, align 4
  %times1 = mul i64 %8, 2
  %plus2 = add i64 %7, %times1
  store i64 %plus2, i64* %curr, align 4
  %9 = load i64, i64* %curr, align 4
  %10 = load i64, i64* %val, align 4
  %plus3 = add i64 %9, %10
  store i64 %plus3, i64* %curr, align 4
  %11 = load i64, i64* %curr, align 4
  %write4 = call i64 @write(i64 %11)
  %12 = load i64, i64* %i, align 4
  %i5 = add i64 %12, 1
  store i64 %i5, i64* %i, align 4
  br label %for.condition

exit:                                             ; preds = %for.condition
  store i64 0, i64* %index, align 4
  br label %while.cond

while.cond:                                       ; preds = %while.body, %exit
  %13 = load i64, i64* %index, align 4
  %14 = load i64, i64* %end, align 4
  %cond = icmp slt i64 %13, %14
  br i1 %cond, label %while.body, label %while.end

while.body:                                       ; preds = %while.cond
  %15 = load i64, i64* %index, align 4
  %16 = load i64, i64* %start, align 4
  %times6 = mul i64 1, %16
  %plus7 = add i64 %15, %times6
  store i64 %plus7, i64* %index, align 4
  %17 = load i64, i64* %index, align 4
  %18 = load i64, i64* %val, align 4
  %plus8 = add i64 %17, %18
  store i64 %plus8, i64* %index, align 4
  %19 = load i64, i64* %index, align 4
  %write9 = call i64 @write(i64 %19)
  %incre = add i64 1, %13
  store i64 %incre, i64* %index, align 4
  br label %while.cond

while.end:                                        ; preds = %while.cond
  ret i64 0
}
