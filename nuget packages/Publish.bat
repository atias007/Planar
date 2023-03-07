@echo off

c:
cd\Planar\nuget packages\publish
del *.*

dotnet publish "C:\Planar\nuget packages\Planar.Common\Planar.Common.csproj" --configuration Release --output "c:\Planar\nuget packages\publish"
dotnet publish "C:\Planar\nuget packages\Planar.Job\Planar.Job.csproj" --configuration Release --output "c:\Planar\nuget packages\publish"
dotnet publish "C:\Planar\nuget packages\Planar.Job.Test\Planar.Job.Test.csproj" --configuration Release --output "c:\Planar\nuget packages\publish"
dotnet publish "C:\Planar\nuget packages\Planar.Monitor.Hook\Planar.Monitor.Hook.csproj" --configuration Release --output "c:\Planar\nuget packages\publish"



pause