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
    "Interpret OldShankFiles/Pascals.shank", # 10
    "Interpret OldShankFiles/GCD.shank", # 11
    "Interpret Negative", # 12
    "Interpret String", # 13
    "CompilePractice Minimal", # 14
    "CompilePractice Minimal2 --flat=1" # 15
    #"Expressions",
    #"Overloads/overloadsTest.shank",
    #"Generics/complex",
    #"Generics/simple/records",

    $all_runner = {
        foreach($p in $path_list[1..$($path_list.Length - 1)])
        {
            & $generic_runner $p
        }
    }

    $generic_runner = {
        param($args_str)
        "`n** Running $($args_str) **`n"

        # Split then splat.
        # https://stackoverflow.com/a/19252498/16458003
        $args_list = -split $args_str
        $args_list[1] = "$($ds)$($args_list[1])"
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
