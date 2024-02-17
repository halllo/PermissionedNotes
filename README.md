# PermissionedNotes
Experimenting with OIDC, RBAC, ABAC, and ReBAC.

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
