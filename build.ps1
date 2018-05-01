Param(
    [ValidateNotNullOrEmpty()]
    [string]$Target="Default",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration="Release",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Any CPU", "x64")]
    [string]$Platform="Any CPU"
)

$buildDir=$PSScriptRoot
$solutionDir=$buildDir
$srcDir=[System.IO.Path]::Combine($solutionDir, "src")

Write-Host -ForegroundColor Green "*** Building $Configuration in $solutionDir"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** Build"
dotnet build "$srcDir\Containers.sln"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** Unit tests"
dotnet test "$srcDir\WebApi.Test.Unit\WebApi.Test.Unit.csproj"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** Publish"
dotnet publish "$srcDir\Containers.sln"

Write-Host -ForegroundColor Green ""
Write-Host -ForegroundColor Green "*** Preparing Docker images"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** TODO"

Write-Host -ForegroundColor Green ""
Write-Host -ForegroundColor Green "*** Deploying to Kubernetes"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** TODO"
