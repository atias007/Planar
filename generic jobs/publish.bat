c:
cd\Planar\generic jobs
cd FolderCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Monitoring\FolderCheck
cd..

cd HealthCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Monitoring\HealthCheck
cd..

cd RabbitMQCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Monitoring\RabbitMQCheck
cd..

cd RedisCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Monitoring\RedisCheck
cd..

cd SqlQueryCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Monitoring\SqlQueryCheck
cd..

cd WindowsServiceCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Monitoring\WindowsServiceCheck
cd..

cd FolderRetention
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Operation\FolderRetention
cd..

cd RedisOperations
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Operation\RedisOperations
cd..

cd SqlTableRetention
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Operation\SqlTableRetention
cd..

cd WindowsServiceRestart
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Operation\WindowsServiceRestart
cd..

cd InfluxDBCheck
dotnet publish -c Release -o C:\temp\Planar\publish\GenericJobs\Operation\InfluxDBCheck
cd..