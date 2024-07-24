INNER_LOOP_LEN = 44
OUTER_LOOP_LEN = 70000000

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
