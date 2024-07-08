function cst
{
    param([string] $compile, [int]$x, [string]$pls, [switch]$y)
    # path for Shank Project file
    $outputfolder = "./TestCompileOutput"
    if (!(Test-Path -Path $outputfolder))
    {
        mkdir $outputfolder
        echo "creating output path for compiler"
    }
    $sp = './Shank/Shank.csproj'

    # path for Dot Shank folder
    $ds = './Shank/dotShank/'
    $Compile_path_list = "NotAPath", # 0
    "Compile ModuleTest2 --print-ir --linker clang -o ./TestCompileOutput/ModuleTest2.exe", # 1
    "Compile Records/simple --print-ir --linker clang -o ./TestCompileOutput/RecordsTest.exe", # 2
    "Compile RealRange --print-ir --linker clang -o ./TestCompileOutput/RangeTest.exe", # 3
    "Compile Arrays/sum --print-ir --linker clang -o ./TestCompileOutput/ArraySum.exe", # 4
    "Compile Records/nested --linker clang  --print-ir  -o ./TestCompileOutput/NestedRecords.exe", # 5
    "Compile Globals --linker clang -o ./TestCompileOutput/Globals.exe", # 6
    "Compile UnitTestTests1 -u", # 7
    "Compile Builtins/Write --print-ir --linker clang -o TestCompileOutput/PrintTest.exe"# 8
    "Compile Enums", # 9
    "Compile TestLlvmTheoAndMendel -S --print-ir " # 10

    $Output_path_List = "NotAPath",
    "TestCompileOutput/ModuleTest2.exe",
    "TestCompileOutput/RecordsTest.exe",
    "TestCompileOutput/ArraySum.exe",
    "TestCompileOutput/NestedRecords.exe",
    "TestCompileOutput/Globals.exe",
    "TestCompileOutput/PrintTest.exe",
    "TestCompileOutput/PrintTest.exe"


    $interpret_list = "NotAPath", # 0
    "Interpret ModuleTest2", # 1
    "Interpret Records/simple", # 2
    "Interpret RealRange", # 3
    "Interpret Arrays/sum", # 4
    "Interpret Records/nested", # 5
    "Interpret Globals", # 6
    "Interpret UnitTestTests1 -u", # 7
    "Interpret Builtins/Write", # 8
    "Interpret Enums", # 9
    "Compile TestLlvmTheoAndMendel -S --print-ir" # 10
    $all_runner = {
        param([String[]]$arg_list)
        $new_path_list = $arg_list[1..$( $arg_list.Length - 1 )]
        foreach ($p in $new_path_list)
        {
            & $generic_runner -args_str $p -i ($new_path_list.IndexOf($p) + 1) -list_len $arg_list.Length
        }
    }

    $generic_runner = {
        param($args_str, $i, $list_len)

        $args_list = -split $args_str
        $args_list[1] = "$( $ds )$( $args_list[1] )"

        $progress = if ($null -ne $i)
        {
            "( Program $i of $( $list_len - 1 ) )"
        }
        else
        {
            '( Program 1 of 1 )'
        }

        $shank_files = Get-ChildItem $args_list[1] -r -filter *.shank

        foreach ($sf in $shank_files)
        {
            Write-Host "`n**** File Path $progress ****`n" -ForegroundColor green
            Write-Host $sf.FullName
            Write-Host "`n**** File Contents $progress ****`n" -ForegroundColor cyan
            Get-Content $sf.FullName
        }
        Write-Host "`n**** Command $progress ****`n" -ForegroundColor magenta
        "dotnet run $( $args_list -join ' ' ) --project $sp"
        Write-Host "`n**** Output $progress ****`n" -ForegroundColor blue
        dotnet run @args_list --project $sp
    }

    # invoke a script block based on param
    if ( $compile.Equals("c"))
    {
        switch ($x)
        {
            { $_ -eq 0 } {
                & $all_runner $Compile_path_list
            }
            { $_ -lt -1 -or $_ -ge $Compile_path_list.Length } {
                "Bad Argument"
            }
            default {
                echo $_
                & $generic_runner $Compile_path_list[$_]
            }
        }

    }
    else
    {
        if ( $compile.Equals("i"))
        {
            switch ($x)
            {
                { $_ -eq 0 } {
                    & $all_runner $interpret_list
                }
                { $_ -lt -1 -or $_ -ge $interpret_list.Length } {
                    "Bad Argument"
                }
                default {
                    echo $_
                    & $generic_runner $interpret_list[$_]
                }
            }
        }
    }


}