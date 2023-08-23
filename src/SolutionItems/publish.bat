@ECHO --------------------------------------------------------------------------------------
@ECHO Clear Publish Folder
@ECHO --------------------------------------------------------------------------------------
del /q "c:\temp\Planar\publish\*"
rmdir "c:\temp\Planar\publish\planar-cli_contained_x86" /S /Q
rmdir "c:\temp\Planar\publish\planar-cli_contained_x64" /S /Q
rmdir "c:\temp\Planar\publish\planar-cli_x86" /S /Q
rmdir "c:\temp\Planar\publish\planar-cli_x64" /S /Q
rmdir "c:\temp\Planar\publish\planar" /S /Q
rmdir "c:\temp\Planar\publish\planar_contained_x64" /S /Q
rmdir "c:\temp\Planar\publish\database_migrations" /S /Q


@ECHO --------------------------------------------------------------------------------------
@ECHO Publish cli project
@ECHO --------------------------------------------------------------------------------------
cd ..\Planar.CLI
dotnet publish -p:PublishProfile=Properties\PublishProfiles\win-x64.pubxml
dotnet publish -p:PublishProfile=Properties\PublishProfiles\win-x86.pubxml

cd ..\DatabaseMigrations
dotnet publish -c release -o C:\temp\Planar\publish\database_migrations

cd ..\Planar
dotnet publish -c release -o C:\temp\Planar\publish\planar
dotnet publish -c release -o C:\temp\Planar\publish\planar_contained_x64 --self-contained true

pause
@ECHO --------------------------------------------------------------------------------------
@ECHO Delete Files
@ECHO --------------------------------------------------------------------------------------
cd\temp\Planar\publish
cd planar-cli_x64
del *.pdb
del planar-cli
cd..
cd planar-cli_x86
del *.pdb
del planar-cli


@ECHO --------------------------------------------------------------------------------------
@ECHO Create ZIP Files
@ECHO --------------------------------------------------------------------------------------
cd..
cd planar-cli_x64
"C:\Program Files\7-Zip\7z.exe" a ..\planar-cli_x64.zip planar-cli.exe 

cd..
cd planar-cli_x86
"C:\Program Files\7-Zip\7z.exe" a ..\planar-cli_x86.zip planar-cli.exe 

cd..
cd database_migrations
"C:\Program Files\7-Zip\7z.exe" a ..\database_migrations.zip *.*

cd..
cd planar_contained_x64
"C:\Program Files\7-Zip\7z.exe" a ..\planar_contained_x64.zip *.*

cd..
cd planar
"C:\Program Files\7-Zip\7z.exe" a ..\planar.zip *.*

@ECHO --------------------------------------------------------------------------------------
@ECHO Delete Publish Folders
@ECHO --------------------------------------------------------------------------------------

rmdir "c:\temp\Planar\publish\planar-cli_x86" /S /Q
rmdir "c:\temp\Planar\publish\planar-cli_x64" /S /Q
rmdir "c:\temp\Planar\publish\planar" /S /Q
rmdir "c:\temp\Planar\publish\planar_contained_x64" /S /Q
rmdir "c:\temp\Planar\publish\database_migrations" /S /Q

@ECHO --------------------------------------------------------------------------------------
@ECHO Finish
@ECHO --------------------------------------------------------------------------------------
pause
