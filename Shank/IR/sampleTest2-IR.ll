; ModuleID = 'main'
source_filename = "main"

declare i64 @write(i64)

define i64 @start() {
entry:
  %min = alloca i64, align 4
  store i64 0, i64* %min, align 4
  %max = alloca i64, align 4
  store i64 10, i64* %max, align 4
  %a = alloca i64, align 4
  %b = alloca i64, align 4
  %total = alloca i64, align 4
  %counter = alloca i64, align 4
  %0 = load i64, i64* %min, align 4
  store i64 %0, i64* %counter, align 4
  br label %while.cond

while.cond:                                       ; preds = %while.body, %entry
  %1 = load i64, i64* %counter, align 4
  %2 = load i64, i64* %max, align 4
  %cond = icmp slt i64 %1, %2
  br i1 %cond, label %while.body, label %while.end

while.body:                                       ; preds = %while.cond
  %3 = load i64, i64* %counter, align 4
  %times = mul i64 %3, 2
  store i64 %times, i64* %total, align 4
  %4 = load i64, i64* %counter, align 4
  %plus = add i64 %4, 1
  store i64 %plus, i64* %counter, align 4
  %5 = load i64, i64* %total, align 4
  %write = call i64 @write(i64 %5)
  %incre = add i64 1, %1
  store i64 %incre, i64* %counter, align 4
  br label %while.cond

while.end:                                        ; preds = %while.cond
  %6 = load i64, i64* %min, align 4
  store i64 %6, i64* %a, align 4
  br label %for.condition

for.condition:                                    ; preds = %for.body, %while.end
  %7 = load i64, i64* %a, align 4
  %8 = load i64, i64* %max, align 4
  %loop.condition.cmp = icmp slt i64 %7, %8
  br i1 %loop.condition.cmp, label %for.body, label %exit

for.body:                                         ; preds = %for.condition
  %9 = load i64, i64* %a, align 4
  %10 = load i64, i64* %a, align 4
  %times1 = mul i64 %9, %10
  store i64 %times1, i64* %total, align 4
  %11 = load i64, i64* %total, align 4
  %write2 = call i64 @write(i64 %11)
  %12 = load i64, i64* %a, align 4
  %i = add i64 %12, 1
  store i64 %i, i64* %a, align 4
  br label %for.condition

exit:                                             ; preds = %for.condition
  %13 = load i64, i64* %max, align 4
  store i64 %13, i64* %counter, align 4
  br label %while.cond3

while.cond3:                                      ; preds = %while.body4, %exit
  %14 = load i64, i64* %min, align 4
  %15 = load i64, i64* %counter, align 4
  %cond6 = icmp slt i64 %14, %15
  br i1 %cond6, label %while.body4, label %while.end5

while.body4:                                      ; preds = %while.cond3
  %16 = load i64, i64* %total, align 4
  %17 = load i64, i64* %counter, align 4
  %minus = sub i64 %16, %17
  store i64 %minus, i64* %total, align 4
  %18 = load i64, i64* %counter, align 4
  %minus7 = sub i64 %18, 1
  store i64 %minus7, i64* %counter, align 4
  %19 = load i64, i64* %total, align 4
  %write8 = call i64 @write(i64 %19)
  %incre9 = add i64 1, %14
  store i64 %incre9, i64* %min, align 4
  br label %while.cond3

while.end5:                                       ; preds = %while.cond3
  %20 = load i64, i64* %max, align 4
  store i64 %20, i64* %b, align 4
  br label %for.condition10

for.condition10:                                  ; preds = %for.body11, %while.end5
  %21 = load i64, i64* %b, align 4
  %22 = load i64, i64* %min, align 4
  %loop.condition.cmp16 = icmp slt i64 %21, %22
  br i1 %loop.condition.cmp16, label %for.body11, label %exit17

for.body11:                                       ; preds = %for.condition10
  %23 = load i64, i64* %max, align 4
  %24 = load i64, i64* %b, align 4
  %25 = load i64, i64* %max, align 4
  %26 = load i64, i64* %b, align 4
  %divide = sdiv i64 %25, %26
  %plus12 = add i64 %24, %divide
  %times13 = mul i64 %23, %plus12
  store i64 %times13, i64* %total, align 4
  %27 = load i64, i64* %total, align 4
  %write14 = call i64 @write(i64 %27)
  %28 = load i64, i64* %b, align 4
  %i15 = add i64 %28, 1
  store i64 %i15, i64* %b, align 4
  br label %for.condition10

exit17:                                           ; preds = %for.condition10
  ret i64 0
}
