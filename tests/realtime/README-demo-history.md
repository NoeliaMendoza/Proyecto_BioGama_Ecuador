# Historial de k8s-v3: demostracion reemplazada por stock en vivo

Este documento y verification.txt son evidencia historica. El boton SendTest
fue retirado en k8s-v4. Seguir README.md para la implementacion actual.

Abrir http://127.0.0.1:30080/Realtime en dos navegadores. El cliente usa
SignalR sobre WebSockets, sin negociacion HTTP. Esto permite mantener cada
conexion en su pod sin requerir afinidad de sesion para la negociacion.
Recargar una ventana si ambas muestran el mismo pod y se quiere evidenciar
la comunicacion entre replicas distintas.

Flujo de demostracion: cliente A -> hub en pod A -> Redis Pub/Sub -> hub
en pod B -> cliente B. El emisor tambien recibe el evento. Comparar el ID
del evento en ambas ventanas, el pod emisor y el pod de cada conexion.

Eventos: Connected identifica el pod de la conexion; Activity lleva ID,
tipo, mensaje fijo, pod emisor y fecha UTC. SendTest genera una prueba,
limitada a una cada dos segundos por conexion. Al crear un registro valido,
RecordsController publica Activity despues de guardar en PostgreSQL; un
fallo de notificacion no revierte ese guardado. No se publican datos del
investigador ni detalles privados del registro.

La pagina de demostracion es publica y el hub permite enviar solo un mensaje
fijo de prueba. Esta limitacion no constituye un limite global contra abuso.
Redis solo tiene Service interno; no se publico su puerto en Windows.

Verificacion automatizada de un mismo evento entre dos pods distintos:

```powershell
.\tests\realtime\verify.ps1
```

En la primera verificacion se obtuvo PASS con conexiones a los pods
biogama-web-59b7fb9989-lh6gh y biogama-web-59b7fb9989-tnwhh. Ambos recibieron
el evento b4e3703afc5b41d5b86ed5664149fe37, emitido por el primero.
Se verifico tambien HTTP 200 en la pagina desde dentro del pod.
La creacion real de un registro y la interfaz visual quedan para comprobacion
manual; el script valida el evento de prueba sobre WebSockets.

Las notificaciones no son una cola durable: pueden perderse si un cliente
esta desconectado o Redis no esta disponible. El cliente intenta reconectar,
pero no recupera el historial. Redis tiene una sola replica en este laboratorio.

Fuente de arquitectura:
https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane?view=aspnetcore-10.0

Cliente JavaScript oficial @microsoft/signalr 10.0.0, descargado de
https://cdn.jsdelivr.net/npm/@microsoft/signalr@10.0.0/dist/browser/signalr.min.js
