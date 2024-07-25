# Setting INNER_LOOP_LEN to 44 causes the inner loop to calculate the 46th
# Fibonacci number: 1836311903
# See: https://planetmath.org/listoffibonaccinumbers
# The 46th Fibonacci number was chosen as the target because it is the largest
# Fibonacci number that is smaller than INT_MAX (2147483647).

INNER_LOOP_LEN = 44
OUTER_LOOP_LEN = 9000000

def fibonacci(m, n):
    curr = 1
    for i in range(m):
        prev1 = 1
        prev2 = 1
        curr = 1
        for j in range(n):
            curr = prev1 + prev2
            prev2 = prev1
            prev1 = curr
    return curr


print(fibonacci(OUTER_LOOP_LEN, INNER_LOOP_LEN))
