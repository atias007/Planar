@echo off 
set version=1.3.0

echo run docker build script for version: %version%
echo ******** CLEAR OLD ********
pause

c:
docker rm planar-service --force
docker rm planar-db --force
docker volume rm planar_dbvolume --force
rmdir /s /q C:\temp\Demo\Planar
copy C:\Planar\docker\docker-compose.yml C:\temp\Demo\docker-compose.yml /Y
copy C:\Planar\docker\docker-compose-up.bat C:\temp\Demo\docker-compose-up.bat /Y

echo ******** BUILD IMAGE ********
pause

cd\Planar\src
docker build . -t planar:%version%
echo ******** TAG IMAGE ********
pause

docker tag planar:%version% atias007/planar:%version%
docker tag planar:%version% atias007/planar:latest

echo ******** PUSH IMAGE ********
pause

docker login
docker push atias007/planar:%version%
docker push atias007/planar:latest
echo ******** RUN DOCKER COMPOSE ********
pause

cd\temp\Demo
docker-compose-up.bat
echo *** DONE ***
pause