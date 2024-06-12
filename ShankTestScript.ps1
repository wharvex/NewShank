function st
{
    param([byte]$x)

    # path for Shank Project file
    $sp = './Shank/Shank.csproj'

    # path for Dot Shank folder
    $ds = './Shank/dotShank/'

    $path_list = "NotAPath", # 0
    "ModuleTest2", # 1
    "Records/simple", # 2
    "Generics/simple/functions2", # 3
    "Arrays/sum", # 4
    "Records/nested", # 5
    "Globals", # 6
    "UnitTestTests1 -ut", # 7
    "Builtins/Write", # 8
    "Enums", # 9
    "OldShankFiles/Pascals.shank", # 10
    "OldShankFiles/GCD.shank", # 11
    "Negative" # 12
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
        $args_list = -split $args_str
        switch ($args_list.Length)
        {
            { $_ -eq 1 } { dotnet run "Interpret" "$($ds)$($args_list[0])" --project $sp }
            { $_ -eq 2 } { dotnet run "Interpret" "$($ds)$($args_list[0])" $args_list[1] --project $sp }
            Default {
                "Bad Args"
            }
        }
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
