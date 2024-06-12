function st
{
    param([byte]$x)

    # path for Shank Project file
    $sp = './Shank/Shank.csproj'

    # path for Dot Shank folder
    $ds = './Shank/dotShank/'

    $path_list = "NotAPath", # 0
    "Interpret ModuleTest2", # 1
    "Interpret Records/simple", # 2
    "Interpret Generics/simple/functions2", # 3
    "Interpret Arrays/sum", # 4
    "Interpret Records/nested", # 5
    "Interpret Globals", # 6
    "Interpret UnitTestTests1 -ut", # 7
    "Interpret Builtins/Write", # 8
    "Interpret Enums", # 9
    "CompilePractice Minimal/PrintInt/Shank --flat PrintInt", # 10
    "Interpret VariableUsageOp" # 11
    #"Interpret OldShankFiles/Pascals.shank"
    #"Interpret OldShankFiles/GCD.shank"
    #"Interpret Negative"
    #"Interpret String"
    #"CompilePractice Minimal/Old"
    #"CompilePractice Minimal/PrintStr/Shank --flat PrintStr"
    #"Expressions"
    #"Overloads/overloadsTest.shank"
    #"Generics/complex"
    #"Generics/simple/records"

    $all_runner = {
        $new_path_list = $path_list[1..$($path_list.Length - 1)]
        foreach($p in $new_path_list)
        {
            & $generic_runner -args_str $p -i ($new_path_list.IndexOf($p) + 1)
        }
    }

    $generic_runner = {
        param($args_str, $i)

        $args_list = -split $args_str
        $args_list[1] = "$($ds)$($args_list[1])"

        $progress = if ($i -ne $null) `
        {"($i of $($path_list.Length - 1))"} `
        Else {''}

        "`n**** Running The Following Command $($progress)****`n"
        "dotnet run $($args_list -join ' ') --project $sp`n"
        dotnet run @args_list --project $sp
    }

    # invoke a script block based on param
    switch ($x)
    {
        { $_ -eq 0 } { & $all_runner }
        { $_ -lt 0 -or $_ -ge $path_list.Length } { "Bad Argument" }
        Default {
            & $generic_runner $path_list[$_] 
        }
    }
}
