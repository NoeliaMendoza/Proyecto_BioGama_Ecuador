# Verificación del chat visitante–administrador

Fecha: 4 de noviembre de 2026, zona America/Guayaquil.
Versiones: web `k8s-v11`, chat `k8s-v1`.

La funcionalidad se adaptó del chat privado de Tarea2 a un servicio Node.js con
Socket.IO. .NET conserva la autenticación de las cuentas; el middleware Node valida
credenciales HMAC temporales. PostgreSQL guarda el historial y Redis distribuye
los eventos entre réplicas.

## Comprobaciones observadas

- 120 pruebas .NET aprobadas, incluyendo firma y vencimiento de credenciales,
  rol asignado desde el servidor, origen configurado y protección antifalsificación.
- Seis pruebas Node aprobadas, incluyendo autenticación, validación, arranque
  y una prueba de integración con dos instancias, PostgreSQL y Redis reales.
  Se verificaron aislamiento,
  firma, Origin, entrega bidireccional, idempotencia y recuperación del historial.
- Tres pruebas de interfaz jsdom aprobadas: texto sin ejecución de HTML, reintento
  con el mismo UUID, selección de conversación y bloqueo de envío sin conexión.
  La suite completa de interfaz incluye 18 pruebas, todas aprobadas.
- Dos perfiles Chrome separados iniciaron sesión mediante Identity real y
  abrieron `/Chat`. La consulta del visitante y la respuesta del administrador
  aparecieron sin recargar.
- Durante el intercambio, el visitante estuvo conectado al pod
  `biogama-chat-5b67c4b7-swxsw` y el administrador a
  `biogama-chat-5b67c4b7-9rlnj`.
- Tras recargar, el visitante se conectó al otro pod y recuperó los dos mensajes
  desde PostgreSQL. No se observaron violaciones CSP en las páginas comprobadas.

## Evidencias locales

En `.local/evidencias-chat/20261104/` se conservan las capturas reales
`01-visitante-chat.png`, `02-administrador-chat.png` y el registro
`verificacion-navegadores.json`. Se usaron cuentas temporales con correos ficticios
de example.invalid; sus contraseñas y tokens no aparecen en las capturas.
Las cuentas y sus conversaciones de prueba se eliminaron después de capturar.

Las pruebas de dos instancias locales y las de dos pods son verificaciones
distintas. La comprobación Chrome no mide rendimiento, disponibilidad prolongada,
seguridad completa ni tolerancia a una caída del nodo. Se requieren nuevas
mediciones y análisis SonarQube/ZAP para evaluar esta incorporación.

La cobertura LCOV medida en esta ejecución fue 87,77 % de líneas para el código
del servicio Node y 92,25 % para `support-chat.js`. Son ámbitos separados;
estos porcentajes no representan la cobertura global de BioGama ni un resultado
actualizado de SonarQube.
