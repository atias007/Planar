@echo

set /p apikey="Enter API key:"
set /p version="Enter version number:"

@echo apikey=%apikey%
@echo version=%version%

@echo -- > Press any key to continue...
pause

c:
Cd\Planar\nuget packages

rmdir "publish" /S /Q

cd Planar.Client
dotnet build Planar.Client.csproj --configuration Release
dotnet pack Planar.Client.csproj --configuration Release --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Common
dotnet build Planar.Common.csproj --configuration Release
dotnet pack Planar.Common.csproj --configuration Release --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Hook
dotnet build Planar.Hook.csproj --configuration Release
dotnet pack Planar.Hook.csproj --configuration Release --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Hook.Test
dotnet build Planar.Hook.Test.csproj --configuration Release
dotnet pack Planar.Hook.Test.csproj --configuration Release --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Job
dotnet build Planar.Job.csproj --configuration Release
dotnet pack Planar.Job.csproj --configuration Release --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Job.Test
dotnet build Planar.Job.Test.csproj --configuration Release
dotnet pack Planar.Job.Test.csproj --configuration Release --output "C:\Planar\nuget packages\publish"
cd..

cd..
cd src\Planar.CLI
dotnet build --configuration Release
dotnet pack --configuration Release --output "C:\Planar\nuget packages\publish"
cd..
cd..


cd nuget packages\publish

@echo -- > Press any key to continue...
pause

dotnet nuget push Planar.Client.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Common.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Hook.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Job.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Hook.Test.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Job.Test.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push planar-cli.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json