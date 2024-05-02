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
dotnet publish --output "C:\Planar\nuget packages\publish"
dotnet pack --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Common
dotnet publish --output "C:\Planar\nuget packages\publish"
dotnet pack --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Hook
dotnet publish --output "C:\Planar\nuget packages\publish"
dotnet pack --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Hook.Test
dotnet publish --output "C:\Planar\nuget packages\publish"
dotnet pack --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Job
dotnet publish --output "C:\Planar\nuget packages\publish"
dotnet pack --output "C:\Planar\nuget packages\publish"
cd..

cd Planar.Job.Test
dotnet publish --output "C:\Planar\nuget packages\publish"
dotnet pack --output "C:\Planar\nuget packages\publish"
cd..

cd publish

@echo -- > Press any key to continue...
pause

dotnet nuget push Planar.Client.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Common.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Hook.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Job.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Hook.Test.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
dotnet nuget push Planar.Job.Test.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
