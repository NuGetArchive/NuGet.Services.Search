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

& "$xunitRoot\xunit.console.exe" @args