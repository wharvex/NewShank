
<#PSScriptInfo

.VERSION 0.1

.GUID 74dda12f-98a2-4210-9ede-50195f3ba3b6

.AUTHOR Tim Gudlewski

.COMPANYNAME

.COPYRIGHT (c) 2024 Tim Gudlewski

.TAGS Dotnet Testing Input Files

.LICENSEURI

.PROJECTURI

.ICONURI

.EXTERNALMODULEDEPENDENCIES

.REQUIREDSCRIPTS

.EXTERNALSCRIPTDEPENDENCIES dotnet run

.RELEASENOTES
First version

#>





<#

.DESCRIPTION
You can run multiple dotnet run commands with your choice of input files and config options.

#>


$config_values = $(Get-Content "./TestScriptConfigValues.txt")

function st {
    [OutputType([String])]
    [CmdletBinding()]
    param(
      [PSDefaultValue(Help='File containing lines of arguments to pass to `dotnet run`')]
      [string] $args_lines_source = $config_values[0],

      [PSDefaultValue(Help='Line number in args lines file to run (default: all lines)')]
      [int] $choice = -1
    )

    $project_file_path = $config_values[1]
    $input_exts = -split $config_values[2]
    $input_file_path_arg_position = $config_values[3]

    $args_lines = $(Get-Content $args_lines_source)

    # script block to run all input files
    $run_all = {
        foreach ($line in $args_lines) {
            & $run_one -args_line $line -i $args_lines.IndexOf($line)
        }
    }

    # script block to run one input file
    $run_one = {
        param($args_line, $i)

        $args_list = -split $args_line

        $progress = if ($null -ne $i) `
            { "( Program $($i + 1) of $($args_lines.Length) )" } `
            else { '( Program 1 of 1 )' }

        $input_files = Get-ChildItem $args_list[$input_file_path_arg_position] `
            -r | Where-Object { $_.Extension -in $input_exts }

        foreach ($file in $input_files ) {
            Write-Host "`n**** Input File Path $progress ****`n" -ForegroundColor green
            Write-Host $file.FullName
            Write-Host "`n**** Input File Contents $progress ****`n" -ForegroundColor cyan
            Get-Content $file.FullName
        }
        Write-Host "`n**** Command $progress ****`n" -ForegroundColor magenta
        "dotnet run $($args_list -join ' ') --project $project_file_path"
        Write-Host "`n**** Output $progress ****`n" -ForegroundColor blue
        dotnet run @args_list --project $project_file_path
    }

    # invoke a script block based on choice
    switch ($choice) {
        { $_ -eq -1 } { & $run_all }
        { $_ -lt -1 -or $_ -gt $args_lines.Length } { "Bad Argument" }
        default {
            & $run_one $args_lines[$_ - 1]
        }
    }
}


