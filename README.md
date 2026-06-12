# Figuras Que Enseñan

Plataforma educativa distribuida para practicar sumas mediante niveles
interactivos y reconocimiento de manos. El sistema registra sesiones y
resultados, permite a tutores consultar el progreso de sus estudiantes y ofrece
un cliente administrativo para estadísticas, logs y monitoreo.

## Funciones principales

- Registro e inicio de sesión para estudiantes, tutores y administradores.
- Niveles educativos con detección de dedos mediante cámara.
- Seguimiento de sesiones, intentos, resultados y avance.
- Asociación de estudiantes con tutores.
- Panel administrativo, logs centralizados y salud de servicios.
- Acceso web HTTPS desde varios equipos de la red.
- Túnel temporal de Cloudflare para pruebas de cámara sin instalar certificados.
- Procesamiento concurrente de manos con detectores aislados por solicitud.

## Arquitectura y tecnologías

| Componente | Tecnologías |
| --- | --- |
| Cliente web | ASP.NET Core Razor Pages, JavaScript, Bootstrap |
| API Gateway | Node.js, Express, OpenAPI, gRPC |
| Autenticación | ASP.NET Core, JWT, PostgreSQL |
| Datos educativos | ASP.NET Core, Entity Framework Core, PostgreSQL |
| Detección de manos | Python, MediaPipe, OpenCV, gRPC |
| Logs | Node.js, WebSocket, MongoDB |
| Mensajería | RabbitMQ y listener Node.js |
| Cliente administrativo | WPF |
| Cliente móvil | Kotlin, Jetpack Compose, Retrofit |
| Despliegue local | Docker Compose, Caddy, Cloudflare Tunnel |
| Pruebas | Newman/Postman y Apache JMeter |

Los clientes entran por Caddy y el Gateway; los servicios y bases de datos no se
publican directamente fuera de la red privada de Docker.

## Arranque rápido

Requiere Windows, PowerShell y Docker Desktop.

```powershell
.\Start-FigurasQE.ps1
```

El script:

1. Detecta la IPv4 principal de la computadora.
2. Construye e inicia todo el stack con Docker Compose.
3. Restaura los respaldos incluidos si la base está vacía.
4. Genera el certificado para HTTPS local.
5. Muestra la URL local y la URL temporal de Cloudflare.

Para pruebas con varios navegadores y dispositivos, usa la dirección
`https://...trycloudflare.com` mostrada al finalizar. La URL actual también se
guarda en `URL-PUBLICA-ACTUAL.txt`.

La primera vez que se usará HTTPS por IP local, ejecuta como administrador:

```powershell
.\Configurar-Firewall.ps1
```

La guía completa está en
[INSTALACION-RED-LOCAL.md](INSTALACION-RED-LOCAL.md).

## Configuración

No existe una IP fija en el código. `Start-FigurasQE.ps1` selecciona la interfaz
física activa y pasa su dirección a Caddy mediante `LAN_IP`.

Para cambiar credenciales de desarrollo o el tamaño del grupo de detectores:

```powershell
Copy-Item .env.example .env
```

Edita `.env`; Git lo ignora. Los valores incluidos por defecto son únicamente
para demostración local y no deben utilizarse en producción.

## Comandos útiles

```powershell
docker compose ps
docker compose logs -f frontend gateway hands-detection
docker compose down
```

Para reconstruir todo:

```powershell
docker compose up -d --build
```

## Estructura

```text
FigurasQE-Frontend/               Cliente web
FigurasQE-Gateway/                Punto de entrada HTTP y puente gRPC
FigurasQE-AuthenticationService/  Autenticación y emisión de JWT
FiguerasQueEnse-anBD/             Servicio de datos educativos
HandsDetection/                   Reconocimiento de manos
FQE.LogsService/                  Consulta de logs
FQE-RabbitMongoListener-Node/     Persistencia asíncrona de eventos
FigurasQueEnsenan.Android/        Cliente móvil
FigurasQueEnseñanAdmin/           Cliente administrativo WPF
documentacion/                    Documento y artefactos del proyecto
newman/ y jmeter/                 Pruebas de API y carga
```

## Documentación

- [Documento final](documentacion/documentacion-proyecto.pdf)
- [Interfaces](documentacion/PDF%20INETRFACES.pdf)
- [Historial de cambios](CHANGELOG.md)
- [Instalación en red local](INSTALACION-RED-LOCAL.md)

## Seguridad

El túnel rápido de Cloudflare es temporal y público. Mientras esté activo,
cualquier persona que conozca la URL puede alcanzar la pantalla inicial. Para un
despliegue real deben cambiarse las credenciales, el secreto JWT y el mecanismo
de publicación.
