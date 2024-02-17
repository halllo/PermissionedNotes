# PermissionedNotes
Experimenting with RBAC and permissions between objects and subjects.

## Getting started
podman run --name postgres2 -e POSTGRES_PASSWORD=secretpassword -e POSTGRES_DB=permissionednotes -d -p 5433:5432 postgres
podman run -d --rm -p 5051:80 --env PGADMIN_DEFAULT_EMAIL=admin@admin.com --env PGADMIN_DEFAULT_PASSWORD=root --name pgadmin2 dpage/pgadmin4:8
podman run --name spice2 -p 50052:50051 --rm authzed/spicedb migrate head --datastore-engine=postgres --datastore-conn-uri="postgres://postgres:secretpassword@localhost:5433/permissionednotes?sslmode=disable"
podman run --name spice2 -d -p 50052:50051 authzed/spicedb serve --grpc-preshared-key "secretkey" --datastore-engine=postgres --datastore-conn-uri="postgres://postgres:secretpassword@localhost:5433/permissionednotes?sslmode=disable"

netstat -ltnp | grep -w ':<port>'

dotnet ef migrations add NotesInit -c NotesDbContext -o Migrations/NotesDb