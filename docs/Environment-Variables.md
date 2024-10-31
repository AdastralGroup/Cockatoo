# Environment Variables

You can modify if Cockatoo should get environment variables from a file, or from the actual environment. You can specify a file with the `COCKATOO_ENV_LOCATION` environment variable.

| Key | Type | Default Value | Required | Description |
| --- | ---- | ------------- | -------- | ----------- |
| `COCKATOO_ENV_LOCATION` | string | | Yes | Load environment variables from the file location specified.
| `CONFIG_ENV_ENABLE` | bool | false | No | If you wish to import your config from an XML file, then set this to `true`.
| `CONFIG_ENV` | string | | No | XML Config location. Required when `CONFIG_ENV_ENABLE` is set to `true`. See [XML Config File Format](#xml-config-file-format) section.
| `INFISICAL_CLIENT_ID` | string | | No | Machine/User Client Id for Infisical.
| `INFISICAL_CLIENT_SECRET` | string | | No | Client Secret for Infisical.
| `INFISICAL_PROJECT_ID` | string | | No | Project Id for Infisical.
| `INFISICAL_ENDPOINT` | string | | No | Endpoint for Infisical. Only specify when not using SaaS Infisical
| `INFISICAL_ENVIRONMENT` | string | | No | Environment to use for Infisical
| `INFISICAL_ENABLED` | bool | false | No | Should [Infisical](https://infisical.com/) be used for configuration values?
| `MONGO_CONNECTION` | string | | Yes | MongoDB Connection String
| `MONGO_DATABASE` | string | | Yes | Database to use
| `PUBLIC_URL` | string | `http://localhost:5280` | Yes | Base URL where Cockatoo is hosted.
| `PARTNER_URL` | string | null | No | Direct URL to this server instance. Should be a URL that can handle request body of >1gb
| `AUTH_BASIC_ENABLE` | bool | `false` | No |
| `AUTHENTIK_ENABLE` | bool | `false` | No | Set to `true` if you want to have Authentik available for authentication.
| `AUTHENTIK_CLIENT_ID` | string | | Yes | Client ID for authentication server
| `AUTHENTIK_CLIENT_SECRET` | string | | Yes | Client Secret for Authentik
| `AUTHENTIK_SLUG` | string | | Yes | Authentik Application Slug
| `AUTHENTIK_DOMAIN` | string | | Yes | Authentik Server Domain (like `auth.example.com`)
| `LDAP_ENABLE` | bool | `false` | No |
| `LDAP_SERVER` | string | | No |
| `LDAP_PORT` | int | `389` | No |
| `LDAP_SECURE` | bool | `false` | No |
| `LDAP_SSL` | bool | `false` | No |
| `LDAP_BASEDN` | string | | No |
| `LDAP_SEARCH_FILTER` | string | | No |
| `LDAP_SEARCH_FILTER_ATTRIBUTES` | string | | No |
| `LDAP_ATTRIBUTES` | string[] | | No |
| `LDAP_REQUIRED_GROUP` | string | | No |
| `LDAP_FORMAT_USERNAME` | string | `$1` | No |
| `LDAP_FORMAT_GROUP` | string | `$1` | No |
| `LDAP_SERVICE_ACCOUNT_USERNAME` | string | | No |
| `LDAP_SERVICE_ACCOUNT_PASSWORD` | string | | No |
| `COCKATOO_STORAGE_LOCAL` | bool | `false` | No | Use local file storage for uploading files.
| `COCKATOO_STORAGE_LOCATION` | string | `./storage` | No | Base directory for local storage. Only used when `COCKATOO_STORAGE_LOCAL` is set to `true`
| `COCKATOO_STORAGE_PROXY` | string | `false` | No | When enabled, the content for files will be proxied through Cockatoo instead of doing a 302 redirect (Only used when `COCKATOO_STORAGE_LOCAL` is `false`)
| `COCKATOO_ENDPOINT_FILE` | string | | Yes | HTTP endpoint where all files get served from [`FileV1Controller.cs`](Adastral.Cockatoo.Services.WebApi/Controllers/FileApiV1Controller.cs)
| `COCKATOO_S3_ENABLE` | bool | `false` | No | Use an S3-compatible for the Storage Layer.
| `COCKATOO_S3_BUCKET` | string | | Yes (when using S3) | Bucket name to use
| `COCKATOO_S3_ACCESS_KEY` | string | | Yes (when using S3) | S3 Access Key to use
| `COCKATOO_S3_SERVICE_URL` | string | | Yes (when using S3) | AWS S3-Compatible Service URL (e.g; `https://s3.us-west-1.amazonaws.com`)
| `COCKATOO_S3_NOAWS` | bool | `false` | No | When using a non-AWS S3 Service Provider, this must be set to `true`.
| `COCKATOO_S3_AUTH_REGION` | string | | No | When using MinIO (or something alike), make sure that this is set to the region that is configured for the endpoint provided. This will only be respected when `COCKATOO_S3_NOAWS` is enabled.
| `COCKATOO_SOUTHBANK_V1_DLURL` | string | | No | Value for `dl_url` in `/api/v1/Southbank`
| `COCKATOO_SOUTHBANK_V2_DLURL` | string | | No | Value for `dl_url` in `/api/v2/Southbank`
| `COCKATOO_SOUTHBANK_V3_DLURL` | string | | No | Value for `dl_url` in `/api/v3/Southbank`
| `COCKATOO_SENTRY` | bool | `false` | No | Enable Sentry.
| `COCKATOO_SENTRY_DSN` | string | | Yes (when `COCKATOO_SENTRY` is `true`) | Sentry DSN URL. Only required when `COCKATOO_SENTRY` is enabled
| `COCKATOO_SWAGGER_ENABLE` | bool | `false` | No | Enable swagger on production builds.
| `COCKATOO_BEHIND_PROXY` | bool | `false` | No | Is this behind a proxy? If so, then the Request IP Address will be fetched from the `x-forwarded-for` header
| `COCKATOO_HEADER_TOKEN` | string | `x-cockatoo-token` | Yes | Header that is used for token authentication. When this is changed, make sure that `Cockatoo.REST.Client.CockatooRestClientOptions.TokenHeaderName` is changed as well.
| `REDIS_ENABLE` | bool | `false` | Should Redis be used as the provider for `IDistributedCache`. When `false`, `Microsoft.Extensions.Caching.Memory` will be used instead.
| `REDIS_CONNECTION` | string | | Yes (when `REDIS_ENABLE` is true) | Connection string used for Redis. Should be formatted like `HOST:PORT,password=supersecretpassword` ([src](https://redis.io/learn/develop/dotnet#step-3-initialize-the-connectionmultiplexer), [archive.org](http://web.archive.org/web/20240820073321/https://redis.io/learn/develop/dotnet#step-3-initialize-the-connectionmultiplexer), [archive.is](https://archive.is/BlgVJ#step-3-initialize-the-connectionmultiplexer))
| `REDIS_INSTANCE_NAME` | string | `cockatoo` | No | Instance name to use for the Redis server provided.

When loading the configuration, first Infisical is checked, then the XML config file, then the environment variables.

Environment variables will override everything, and the XML file will only override values set in Infisical.

## XML Config File Format
```xml
<?xml version="1.0" encoding="utf-16"?>
<Config xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <MongoDB>
    <ConnectionString />
	  <Database>cockatoo-development</Database>
  </MongoDB>
  <BasicAuth>
    <Enable>false</Enable>
  </BasicAuth>
  <Authentik>
    <Enable>false</Enable>
    <ClientId />
	  <ClientSecret />
	  <BaseUrl>auth.example.com</BaseUrl>
	  <Slug>cockatoo-development</Slug>
  </Authentik>
  <LDAP>
    <Enable>false</Enable>
    <Server />
    <Port>389</Port>
    <UseSsl>false</UseSsl>
    <Secure>false</Secure>
    <BaseDN>ou=users,dc=ldap,dc=example,dc=com</BaseDN>
    <SearchFilter Value="(objectClass=user)">
      <AttributeItem>uid</AttributeItem>
      <AttributeItem>cn</AttributeItem>
      <AttributeItem>mail</AttributeItem>
    </SearchFilter>
    <Formatting>
      <Username>$1</Username>
      <Group>$1</Group>
    </Formatting>
    <Attributes>cn</Attributes>
    <ServiceAccount>
      <Username />
      <Password />
    </ServiceAccount>
  </LDAP>
  <Storage>
    <Local>
	    <Enable>false</Enable>
      <Location>./storage</Location>
	  </Local>
    <FileApi>
	    <Endpoint>https://cdn.example.com</Endpoint>
      <UseDirect>false</UseDirect>
	  </FileApi>
      <S3>
	    <Enable>true</Enable>
	    <Bucket>cockatoo-prod</Bucket>
	    <AccessKeyId />
	    <AccessSecretKey />
	    <ServiceUrl>http://s3.ap-southeast-2.amazonaws.com</ServiceUrl>
	    <NotUsingAWS>false</NotUsingAWS>
	    <AuthenticationRegion />
	  </S3>
    <Proxy>false</Proxy>
  </Storage>
  <AspNET>
    <EnableSwagger>false</EnableSwagger>
    <TokenHeader>x-cockatoo-token</TokenHeader>
    <BehindProxy>false</BehindProxy>
  </AspNET>
  <Southbank>
    <v1>https://dl.example.com/v1/</v1>
    <v2>https://dl.example.com/v2/</v2>
    <v3>https://dl.example.com/v2/</v3>
  </Southbank>
  <Redis>
    <Enable>true</Enable>
    <ConnectionString>127.0.0.1:6379</ConnectionString>
    <InstanceName>cockatoo-prod</InstanceName>
  </Redis>
  <PublicUrl>https://cdn.example.com</PublicUrl>
  <PartnerUrl>https://partner.cdn.example.com</PartnerUrl>
</Config>
```