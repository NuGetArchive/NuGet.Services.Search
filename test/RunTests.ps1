param(
    [Parameter(Mandatory=$false)][string]$ServiceRoot,
    [Parameter(Mandatory=$false)][string]$Configuration = "Debug",
    [Parameter(Mandatory=$false)][string]$OutputDir,
    [Parameter(Mandatory=$false)][switch]$Quiet,
    [Parameter(Mandatory=$false)][switch]$TeamCity,
    [Parameter(Mandatory=$false)][switch]$SkipBuild)

$tcFailed = [regex]"##teamcity\[testFailed name='(?<name>[^']*)' details='(?<detail>[^']*)'.*\]";
$tcComplete = [regex]"##teamcity\[testFinished name='(?<name>[^']*)' duration='(?<duration>\d+)'.*\]";

$TestProjects = @(
    "NuGet.Services.Search.Test")


if(!$SkipBuild) {
    Write-Host -ForegroundColor Green "** Building **"
    # Build first
    try {
        if($Quiet) {
            & "$PSScriptRoot\Build.ps1" -Configuration $Configuration | Out-Null
        }
        else {
            & "$PSScriptRoot\Build.ps1" -Configuration $Configuration
        }
    } catch {
        throw "Build Failed"
    }
}

# Define Environment Variables
$oldVal = $null;
if ($ServiceRoot) {
    $oldVal = $env:NUGET_TEST_SERVICEROOT
    $env:NUGET_TEST_SERVICEROOT=$ServiceRoot
}

# Run Tests
$failures = @{};
$TestProjects | ForEach-Object {
    $proj = $_
    Write-Host -ForegroundColor Green "** Running Tests in $proj **"
    $additionalArgs = @()
    if($OutputDir) {
        if(!(Test-Path $OutputDir)) {
            mkdir $OutputDir | Out-Null
        }
        $additionalArgs += "-xml"
        $additionalArgs += "$OutputDir\$proj.xml"
        $additionalArgs += "-html"
        $additionalArgs += "$OutputDir\$proj.html"
    }

    $psstandardmembers = [System.Management.Automation.PSMemberInfo[]](New-Object System.Management.Automation.PSPropertySet DefaultDisplayPropertySet,([string[]]@("Test","Result","Time","Failure")))

    & "$PSScriptRoot\xunit.console.ps1" "$PSScriptRoot\$_\bin\$Configuration\$_.dll" @additionalArgs -teamcity | ForEach-Object {
        # Process Lines starting "##teamcity"
        $match = $tcFailed.Match($_)
        if($match.Success) {
            $failures[$match.Groups["name"].Value] = $match.Groups["detail"].Value.Replace("|r", "`r").Replace("|n", "`n")
        }
        else {
            $match = $tcComplete.Match($_)
            if($match.Success) {
                $fullName = $match.Groups["name"].Value
                $shortName = $fullName;
                if($fullName.StartsWith("$proj.")) {
                    $shortName = $fullName.Substring($proj.Length + 1)
                }
                $duration = [int]$match.Groups["duration"].Value
                $failure = $failures[$fullName]
                $result = "Pass"
                if($failure) {
                    $result = "Fail"
                }
                [PSCustomObject]@{
                    Test = $shortName;
                    FullTestName = $fullName;
                    Result = $result;
                    Time = [System.TimeSpan]::FromMilliseconds($duration);
                    Failure = $failure;
                } | Add-Member -MemberType MemberSet -Name PSStandardMembers -Value $psstandardmembers -PassThru
            }
        }
        
        if ($TeamCity -or (!$Quiet -and !$_.StartsWith("##teamcity"))) {
            # Write the line out
            $_ | Out-Host
        }
    }
}

if($failures.Count -gt 0) {
    throw "Test failures encountered!"
}

# Clean up environment
if($oldVal) {
    $env:NUGET_TEST_SERVICEROOT = $oldVal
}