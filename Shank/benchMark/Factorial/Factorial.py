def factorial(n):
    n1 = 1
    i = 1
    while i <= n:
        n1 = n1 * i
        i = i + 1
    return n1
(factorial(10000))