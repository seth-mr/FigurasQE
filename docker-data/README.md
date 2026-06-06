# Docker data backups

This folder contains small backups used to recreate the local infrastructure
state for FigurasQE.

- `postgres/figurasqe.sql`: PostgreSQL schema and seed/current data.
- `mongo/logsdb.archive.gz`: MongoDB `logsdb` archive.
- `rabbitmq/definitions.json`: RabbitMQ queues, exchanges, bindings, vhosts,
  users, and permissions.

To restore them:

```powershell
.\restore-data.ps1
```

The restore uses demo local credentials:

- PostgreSQL: `postgres` / `1234`
- MongoDB: `seth` / `1234`
- RabbitMQ: `guest` / `guest`

To regenerate these backups from local running containers:

```powershell
$env:MONGO_SOURCE_PASSWORD = "<your-local-mongo-password>"
.\backup-data.ps1
```
