Param(
    [ValidateNotNullOrEmpty()]
    [string]$Target="Default",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration="Release",

    [ValidateNotNullOrEmpty()]
    [ValidateSet("linux-x64", "win-x64")]
    [string]$Runtime="linux-x64"
)

$startDir=Get-Location
$buildDir=$PSScriptRoot
$solutionDir=$buildDir
$srcDir=[System.IO.Path]::Combine($solutionDir, "src")
$k8sDir=[System.IO.Path]::Combine($solutionDir, "k8s")

Write-Host -ForegroundColor Green "*** Building $Configuration in $solutionDir"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "*** Build"
dotnet build "$srcDir\Containers.sln" --configuration $Configuration

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Unit tests"
dotnet test "$srcDir\WebApi.Test.Unit\WebApi.Test.Unit.csproj" --configuration $Configuration

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Publish"
dotnet publish "$srcDir\Containers.sln" --configuration $Configuration --runtime $Runtime --output _publish

Write-Host -ForegroundColor Green ""
Write-Host -ForegroundColor Green "*** Clean up existing deployment"

kubectl delete deploy/containersdemo-client-deployment
kubectl delete deploy/containersdemo-webapi-deployment
kubectl delete svc/containersdemo-webapi-service
kubectl delete svc/containersdemo-mongodb
kubectl delete svc/containersdemo-elasticsearch
docker image prune -f

Write-Host -ForegroundColor Green ""
Write-Host -ForegroundColor Green "*** Deploying to Kubernetes"

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Building WebAPI Docker container"

Set-Location -Path "$srcDir\WebApi"
docker build --tag containers-demo/webapi:develop .

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Building Client Docker container"
Set-Location -Path "$srcDir\Client"
docker build --tag containers-demo/client:develop .

Write-Host -ForegroundColor Yellow ""
Write-Host -ForegroundColor Yellow "***** Kubernetes"
Set-Location -Path $k8sDir
kubectl create -f .\elasticsearch-service.yml
kubectl create -f .\mongodb-service.yml
kubectl create -f .\webapi-deployment.yml
kubectl create -f .\webapi-service.yml
kubectl create -f .\client-deployment.yml

Set-Location $startDir
