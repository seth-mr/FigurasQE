# Historial de cambios

## 2026-06-12

- Se agregó HTTPS local con Caddy para habilitar la cámara fuera de `localhost`.
- Se agregó un túnel temporal de Cloudflare para pruebas desde varios clientes.
- El arranque detecta automáticamente la IPv4 principal y ya no depende de una
  dirección personal escrita en el código.
- Se incorporaron scripts para firewall e instalación del certificado local.
- El frontend consume el Gateway mediante la ruta relativa `/api`.
- La detección de manos usa un grupo de instancias independientes de MediaPipe,
  evitando que la mano de un cliente bloquee la detección de otro.
- Se añadió configuración por variables de entorno y `.env.example`.
- Se agregó documentación general de instalación, arquitectura y operación.
