![Logo](https://github.com/atias007/Planar/blob/437265e8b34d94e09bb81f1aa7d9b28103d3ed14/res/planar_logo_full.png)
***

Planar is an enterprise scheduler service. 
It’s providing peripheral tools and infrastructure to met all of schedule jobs needs (small to enterprise systems).\
Scheduler engine is based on [Quartz.NET](http://www.quartz-scheduler.org/)

Below you can see the main key features of Planar:

![flyer](https://github.com/atias007/Planar/blob/8c7222d689590a97414b987517000ce4dc1ef526/res/characters.jpg)

---

### Get Started

You can host and manage Planar directly on your pc by using Docker.

Docker is an open source [containerization](https://www.ibm.com/in-en/cloud/learn/containerization) platform. It enables developers to package applications into standardized executable components called containers

1. Create an installation folder called `Planar` for deployment and data storage.
2. `cd` into the installation folder.
3. Download the [docker-compose.yml](https://github.com/atias007/Planar/releases/download/version_1.2.0/docker-compose.yml) file and place it into the `Planar` installation folder.
4. Start the Docker container by running the command below.[^1][^2]
 
   `docker-compose -p planar up -d`

   [^1]: This may need to be run with sudo if docker and docker-compose aren't accessible by the user. 
   [^2]: If the image doesn't exist locally, this command downloads the necessary Docker image and starts the container.

---

### Test The Installation

1. Open terminal or command line
2. Connect to service container shell by running the command below

   ```PowerShell
   docker exec -it planar-service sh
   ```
3. Run the Planae CLI by running the command below

   ```PowerShell
   planar-cli
   ```
