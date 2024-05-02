c:
cd\Planar\generic jobs
cd FolderCheck
dotnet publish -c Release -o C:\temp\Planar\publish\FolderCheck
cd..

cd HealthCheck
dotnet publish -c Release -o C:\temp\Planar\publish\HealthCheck
cd..

cd RabbitMQCheck
dotnet publish -c Release -o C:\temp\Planar\publish\RabbitMQCheck
cd..

cd RedisCheck
dotnet publish -c Release -o C:\temp\Planar\publish\RedisCheck
cd..

cd SqlQueryCheck
dotnet publish -c Release -o C:\temp\Planar\publish\SqlQueryCheck
cd..