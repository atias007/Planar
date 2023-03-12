@echo off 
set version=1.2.0

echo run docker build script for version: %version%
pause

c:
docker rm planar-service --force
docker rm planar-db --force
docker volume rm planar_dbvolume --force
rmdir /s /q C:\temp\Demo\Planar
copy C:\Planar\docker\docker-compose.yml C:\temp\Demo\docker-compose.yml /Y
copy C:\Planar\docker\docker-compose-up.bat C:\temp\Demo\docker-compose-up.bat /Y

cd\Planar\src
pause

docker build . -t planar:%version%
pause

docker tag planar:%version% atias007/planar:%version%
docker tag planar:%version% atias007/planar:latest
pause

docker login
docker push atias007/%version%
docker push atias007/latest
pause

echo *** DONE ***
pause