function st {
    param([int]$x, [string]$pls, [switch]$y)

    # path for Shank Project file
    $sp = './Shank/Shank.csproj'

    # path for Dot Shank folder
    $ds = './Shank/dotShank/'

    if ($y) {
        $path_list = $(Get-Content "./ShankTestPaths_$($pls).txt")
    }
    else {
        $path_list = "NotAPath", # 0
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
        #"CompilePractice Minimal/PrintInt/Shank --flat PrintInt"
        #"Interpret Generics/simple/functions2" ** revisit this to debug **
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
    }

    $all_runner = {
        $new_path_list = $path_list[1..$($path_list.Length - 1)]
        foreach ($p in $new_path_list) {
            & $generic_runner -args_str $p -i ($new_path_list.IndexOf($p) + 1)
        }
    }

    $generic_runner = {
        param($args_str, $i)

        $args_list = -split $args_str
        $args_list[1] = "$($ds)$($args_list[1])"

        $progress = if ($null -ne $i) `
        { "( Program $i of $($path_list.Length - 1) )" } `
            else { '( Program 1 of 1 )' }

        $shank_files = Get-ChildItem $args_list[1] -r -filter *.shank

        foreach ( $sf in $shank_files )
        {
            Write-Host "`n**** File Path $progress ****`n" -ForegroundColor green
            Write-Host $sf.FullName
            Write-Host "`n**** File Contents $progress ****`n" -ForegroundColor cyan
            Get-Content $sf.FullName
        }
        Write-Host "`n**** Command $progress ****`n" -ForegroundColor magenta
        "dotnet run $($args_list -join ' ') --project $sp"
        Write-Host "`n**** Output $progress ****`n" -ForegroundColor blue
        dotnet run @args_list --project $sp
    }

    # invoke a script block based on param
    switch ($x) {
        { $_ -eq 0 } { & $all_runner }
        { $_ -lt -1 -or $_ -ge $path_list.Length } { "Bad Argument" }
        default {
            & $generic_runner $path_list[$_]
        }
    }
}


