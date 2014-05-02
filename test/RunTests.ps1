param(
    [Parameter(Mandatory=$true)][string]$ServiceRoot,
    [Parameter(Mandatory=$false)][string]$Configuration = "Debug",
    [Parameter(Mandatory=$false)][string]$OutputDir,
    [Parameter(Mandatory=$false)][switch]$Quiet)

$TestProjects = @(
    "NuGet.Services.Search.Test")


Write-Host -ForegroundColor Green "** Building **"
# Build first
if($Quiet) {
    & "$PSScriptRoot\Build.ps1" -Configuration $Configuration | Out-Null
}
else {
    & "$PSScriptRoot\Build.ps1" -Configuration $Configuration
}

# Find xunit runner
$packagesConfigPath = "$PSScriptRoot\.nuget\packages.config"
if(!(Test-Path $packagesConfigPath)) {
    throw "Could not find packages config at $packagesConfigPath!"
}
$packagesConfig = [xml](cat "$PSScriptRoot\.nuget\packages.config")
$version = $packagesConfig.packages.package | where{ $_.id -eq "xunit.runners" } | select -ExpandProperty version
if(!$version) {
    throw "Could not identify installed version of xunit.runners!"
}
$xunitRoot = "$PSScriptRoot\packages\xunit.runners.$version\tools"

# Define Environment Variables
$oldVal = $env:NUGET_TEST_SERVICEROOT
$env:NUGET_TEST_SERVICEROOT=$ServiceRoot

# Run Tests
$TestProjects | ForEach-Object {
    Write-Host -ForegroundColor Green "** Running Tests in $_ **"
    $additionalArgs = @()
    $reportOutput = $null;
    $outputTemp = $false;
    if($OutputDir) {
        if(!(Test-Path $OutputDir)) {
            mkdir $OutputDir | Out-Null
        }
        $reportOutput = "$OutputDir\$_.xml"
        $additionalArgs += "-xml"
        $additionalArgs += $reportOutput
        $additionalArgs += "-html"
        $additionalArgs += "$OutputDir\$_.html"
    } else {
        $reportOutput = [System.IO.Path]::GetTempFileName()
        $outputTemp = $true;
        $additionalArgs += "-xml"
        $additionalArgs += $reportOutput
    }

    if($Quiet) {
        & "$xunitRoot\xunit.console.exe" "$PSScriptRoot\$_\bin\$Configuration\$_.dll" @additionalArgs | Out-Null
    } else {
        & "$xunitRoot\xunit.console.exe" "$PSScriptRoot\$_\bin\$Configuration\$_.dll" @additionalArgs
    }

    $report = [xml](cat $reportOutput)
    if($outputTemp) {
        del $reportOutput;
    }

    $psstandardmembers = [System.Management.Automation.PSMemberInfo[]](New-Object System.Management.Automation.PSPropertySet DefaultDisplayPropertySet,([string[]]@("Test","Result","Time","Failure")))

    $report.assemblies.assembly.collection.test | foreach {
        [PSCustomObject]@{
            Test = $_.name;
            Result = $_.result;
            Time = [System.TimeSpan]::FromSeconds($_.time);
            Failure = $_.failure.message.InnerText;
        } | Add-Member -MemberType MemberSet -Name PSStandardMembers -Value $psstandardmembers -PassThru
    }
}

# Clean up environment
$env:NUGET_TEST_SERVICEROOT = $oldVal