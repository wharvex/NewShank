
int main()
{
    int px = 100;
    int py = 100;
    double x0 = 0.47;
    double y0 = -1.0;
    for (int ix = 0; ix < px; ix++)
    {
        long double x = 0;
        long double y = 0;
        for (int iy = 0; iy < py; iy++)
        {
            long max_it = 100;
            long double tmp = 0;
            tmp = x * x + y * y;
            while (tmp <= 4.0)
            {
                for (int idx = 0; idx < max_it; idx++)
                {
                    long double xtemp = x * x - y * y + x0;
                    y = 2.0 * x * y + y0;
                    x = xtemp;
                    tmp = x * x + y * y;

                }
                
            }
        }
    }
}
