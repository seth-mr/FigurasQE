# Docker - HandsDetection

Este proyecto levanta el servidor gRPC de deteccion de manos en Docker.

## Que hace

El contenedor ejecuta `server.py`, descarga el modelo `hand_landmarker.task` durante el build y compila `fingers.proto` para generar los archivos gRPC de Python.

## Levantar o actualizar

Desde `C:\fig\HandsDetection`:

```powershell
powershell -ExecutionPolicy Bypass -File .\rebuild-docker.ps1
```

## Puerto

El servidor gRPC queda publicado en:

```text
localhost:50051
```

## Integracion con Gateway

El gateway debe apuntar a:

```text
FINGER_GRPC_TARGET=host.docker.internal:50051
```

Ese valor ya permite que el gateway en Docker llegue al puerto publicado en Windows.
