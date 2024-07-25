# TODO: make matrix class
def initMatrix(seed):
    return [[seed * (i + j) for j in range(585)] for i in range(585)]
a = initMatrix(1)
b = initMatrix(3)
c = [[0 for x in range(585)] for x in range(585)]

def matrixMultiply(a,b,c):
    for i in range(585):
        for j in range(585):
            sum = 0
            for k in range(585):
                sum = sum + a[i][k] * b[k][j]
        c[i][j] = sum
matrixMultiply(a,b,c)
