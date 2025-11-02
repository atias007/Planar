@echo

set /p apikey="Enter API key:"
set /p version="Enter version number:"

@echo apikey=%apikey%
@echo version=%version%

@echo -- > Press any key to continue...
pause

c:
Cd\Planar\src\Planar.CLI

rmdir "publish" /S /Q

dotnet build --configuration Release
dotnet pack --configuration Release --output "C:\Planar\nuget packages\publish"

Cd\Planar\nuget packages\publish

@echo -- > Press any key to continue...
pause

dotnet nuget push planar-cli.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json