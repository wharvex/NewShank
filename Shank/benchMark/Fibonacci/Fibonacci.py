def fibnauci(n):
    prev1 = 1
    prev2 = 1
    curr = 1
    for i in range(n):
        curr = prev1 + prev2
        prev2 = prev1
        prev1 = curr
    return curr
        


(fibnauci(100000))
