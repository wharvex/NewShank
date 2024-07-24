# TODO: make matrix class
a = [[0 for x in range(100)] for x in range(100)]
b = [[0 for x in range(100)] for x in range(100)]
c = [[0 for x in range(100)] for x in range(100)]
def matrixMultiply(a,b,c):
    for i in range(100):
        for j in range(100):
            sum = 0
            for k in range(100):
                sum = sum + a[i][k] * b[k][j]
        c[i][j] = sum
matrixMultiply(a,b,c)
