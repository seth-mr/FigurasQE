# FigurasQE en red local

## Arranque

Requisitos:

- Docker Desktop iniciado.
- PowerShell 5.1 o posterior.
- Conexion a Internet si se utilizara el tunel de Cloudflare.

Abre PowerShell en la raiz del repositorio y ejecuta:

```powershell
.\Start-FigurasQE.ps1
```

La primera vez, habilita tambien el puerto local como administrador:

```powershell
.\Configurar-Firewall.ps1
```

El script detecta automaticamente la IPv4 de la interfaz fisica activa, construye
los contenedores y muestra una URL similar a:

```text
https://192.168.1.X:8443
```

No se guarda una IP personal en el repositorio. Si la deteccion selecciona una
interfaz incorrecta, se puede indicar temporalmente:

```powershell
.\Start-FigurasQE.ps1 -LanIp 192.168.1.X
```

Los microservicios permanecen en la red privada de Docker. Hacia la red local
solo se publica el puerto HTTPS `8443`.

## Opcion recomendada para varios clientes

El arranque tambien crea un tunel temporal de Cloudflare y muestra una URL:

```text
https://nombre-aleatorio.trycloudflare.com
```

La URL vigente queda en `URL-PUBLICA-ACTUAL.txt`, archivo que Git ignora porque
cambia en cada arranque. Esta opcion usa un certificado publico reconocido por
Chrome, Edge, Firefox y navegadores moviles, por lo que permite usar la camara
sin instalar certificados en cada cliente.

El tunel requiere Internet y expone temporalmente la aplicacion mientras el
contenedor `public-tunnel` este activo. Debe usarse solamente para pruebas.

## HTTPS local y certificado

Los navegadores solo permiten `getUserMedia()` desde HTTPS o desde `localhost`.
Una direccion IP por HTTP no es un contexto seguro y la camara sera bloqueada.

Despues del primer arranque se crea:

```text
certificados\FigurasQE-CA.crt
```

En cada equipo Windows cliente se puede copiar la carpeta `certificados` y
ejecutar `Instalar-Certificado-Cliente.ps1` como administrador. Despues hay que
cerrar por completo y volver a abrir el navegador.

Instalacion manual:

1. Abre el certificado.
2. Selecciona `Instalar certificado`.
3. Elige `Equipo local`.
4. Selecciona `Entidades de certificacion raiz de confianza`.
5. Finaliza el asistente y reinicia el navegador.
6. Abre la URL HTTPS local y permite el uso de la camara.

En Android, la aceptacion de certificados instalados por el usuario depende del
navegador y de la politica del dispositivo. Para esos equipos se recomienda el
tunel de Cloudflare.

## Configuracion opcional

Copia `.env.example` como `.env` si necesitas cambiar credenciales de desarrollo,
el secreto JWT o el numero de detectores simultaneos. `.env` no se versiona.

## Diagnostico y apagado

```powershell
docker compose ps
docker compose logs https-proxy
docker compose logs public-tunnel
docker compose logs frontend gateway hands-detection
docker compose down
```
