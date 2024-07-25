function run{
    for (($i = 0), ($j = 0); $i -lt 20; $i++)
    {
        measure-command {dotnet run -c Release --project ./../../shank.csproj -- Interpret Fibonacci.shank}
    }
}