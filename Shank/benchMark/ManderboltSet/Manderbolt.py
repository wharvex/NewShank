def manderobolt():
    px = 100
    py = 100
    x0 = -1.0
    y0 = 0.47
    for i in range(px):
        x = 0
        y = 0
        for i2 in range(py):
            max_it = 100
            tmp = x * x + y * y
            while tmp <= 4.0:
                for idx in range(max_it):
                    xtemp = x * x - y * y + x0
                    y = 2.0 * x * y + y0
                    x = xtemp
                    tmp = x * x + y * y
manderobolt()
print("ran")