# PermissionedNotes

Experimenting with OIDC, RBAC, ABAC, and ReBAC.

## Theory

Lets see how the [OWASP Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html) defines these terms.

### Role-Based Access Control

"RBAC is a model of access control in which access is granted or denied based upon the roles assigned to a user. Permissions are not directly assigned to an entity; rather, permissions are associated with a role and the entity inherits the permissions of any roles assigned to it. Generally, the relationship between roles and users can be many-to-many, and roles may be hierarchical in nature."

### Attribute-Based Access Control

"ABAC may be defined as an access control model where "subject requests to perform operations on objects are granted or denied based on assigned attributes of the subject, assigned attributes of the object, environment conditions, and a set of policies that are specified in terms of those attributes and conditions" (NIST SP 800-162, pg. 7]). As defined in NIST SP 800-162, attributes are simply characteristics that be represented as name-value pairs and assigned to a subject, object, or the environment. Job role, time of day, project name, MAC address, and creation date are but a very small sampling of possible attributes that highlight the flexibility of ABAC implementations."

### Relationship-Based Access Control

"ReBAC is an access control model that grants access based on the relationships between resources. For instance, allowing only the user who created a post to edit it. This is especially necessary in social network applications, like Twitter or Facebook, where users want to limit access to their data (tweets or posts) to people they choose (friends, family, followers)."

## Getting started

Running required containers

```bash
podman run --name postgres2 -e POSTGRES_PASSWORD=secretpassword -e POSTGRES_DB=permissionednotes -d -p 5433:5432 postgres
podman run -d --rm -p 5051:80 --env PGADMIN_DEFAULT_EMAIL=admin@admin.com --env PGADMIN_DEFAULT_PASSWORD=root --name pgadmin2 dpage/pgadmin4
#register postgres server by using host ip from podman machine `ip a` because rootless containers use slirp4netns by default: https://github.com/containers/podman/blob/main/docs/tutorials/basic_networking.md#slirp4netns
podman run --name spice2 -p 50052:50051 --rm authzed/spicedb migrate head --datastore-engine=postgres --datastore-conn-uri="postgres://postgres:secretpassword@localhost:5433/permissionednotes?sslmode=disable"
podman run --name spice2 -d -p 50052:50051 authzed/spicedb serve --grpc-preshared-key "secretkey" --datastore-engine=postgres --datastore-conn-uri="postgres://postgres:secretpassword@localhost:5433/permissionednotes?sslmode=disable"
#if container ports dont get freed: netstat -ltnp | grep -w ':<port>' && kill <pid>
```

Using [zed](https://authzed.com/docs/spicedb/getting-started/installing-zed) to inspect SpiceDB

```bash
zed context set local localhost:50052 "secretkey" --insecure #requires setting up a passphrase
ZED_KEYRING_PASSWORD=passphrase #bypass prompt for passphrase on every command
export ZED_KEYRING_PASSWORD
zed schema read
zed relationship read collection
zed relationship read note
```

## Developing

Updating DB schemas

```bash
dotnet ef migrations add Migration1 -c DB -o Migrations
```

## Links

- [SpiceDB Documentation](https://authzed.com/docs/spicedb/getting-started/discovering-spicedb)
- [SpiceDB Schema Playground](https://play.authzed.com/schema)
- [SpiceDB Protobuf Schema](https://buf.build/authzed/api/docs/main:authzed.api.v1)
