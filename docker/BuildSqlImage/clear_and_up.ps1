docker rm planar-service -f
docker rm planar-db -f
docker volume rm planar_dbvolume -f
Remove-Item .\Planar -Force -Recurse
docker-compose -p planar up -d --remove-orphans