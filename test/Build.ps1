param(
    [Parameter(Mandatory=$false)][string]$Configuration = "Debug")

# Find MSBuild
$pf32 = $env:ProgramFiles;
if(Test-Path "env:\ProgramFiles(x86)") {
    $pf32 = (cat "env:\ProgramFiles(x86)")
}

$msbuildSubPath = "MSBuild\12.0\bin\msbuild.exe"
if($env:PROCESSOR_ARCHITECTURE -eq "AMD64") {
    $msbuildSubPath = "MSBuild\12.0\bin\amd64\msbuild.exe";
}

$msbuild = Join-Path $pf32 $msbuildSubPath

# Find the solution
$slns = @(dir "$PSScriptRoot\*.sln")
if($slns.Count -eq 0) {
    throw "No Solution file found!"
} elseif($slns.Count -gt 1) {
    throw "Multiple Solution files found! TODO: Allow disambiguation"
}
$sln = $slns[0];

# Build the solution
&$msbuild $sln.FullName /p:Configuration=$Configuration /p:Platform="Any CPU" /m