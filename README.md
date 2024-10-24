# Cockatoo
Software Distribution tools that is designed for usage with the [Adastral](https://adastralgroup.net) suite of software.

## Requirements
- MongoDB v6.0
- ASP.NET Runtime for .NET 8.0
- Redis/KeyDB (optional)
- OAuth Provider (preferably Authentik), or LDAP Server
- (optional) [Management scripts](https://github.com/ktwrd/cockatoo-management-scripts)

## Docs
- [Environment Variables](docs/Environment-Variables.md)
- [Authentik Setup](docs/OAuth-Authentik.md)
- [ASP.NET Request Body as JSON](docs/JSON-From-Request-Body.md)
- [General Troubleshooting](docs/Troubleshooting.md)
- [Scoped Permissions (for Applications)](docs/Application-Permissions.md)

## Large Files and S3 Proxying
When you are serving large files (>100mb), it is strongly recommended to use an S3-compatible object storage service instead of using the filesystem for storing files. If you do not want to depend on a cloud service for storing your files, then it is a good idea to host a [MinIO](https://min.io) ([docker-compose.yml](https://gist.github.com/ktwrd/2dd80e7b8485bb751fd2e7700af023b7)) instance since it is a self-hostable S3-compatible object storage service.

When using a non-AWS S3 service, make sure that the environment variable `COCKATOO_S3_NOAWS` is set to `true`, otherwise things will not work properly.

If you're using MinIO, then make sure that `COCKATOO_S3_AUTH_REGION` is set to the region of the instance that you're using.
