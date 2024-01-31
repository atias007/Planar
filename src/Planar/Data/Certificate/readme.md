# This folder contains the server certificate

### In short...
This certificate file is used to expose Planar https endpoint.
Place here the `file.pfx` file, then set the following fields in `appsettings.yml` file under `Settings` folder:

> general
> &nbsp;&nbsp;certificate file: file.pfx
> &nbsp;&nbsp;certificate password: my_strong_password_123!@# `(set to null if no password)`
